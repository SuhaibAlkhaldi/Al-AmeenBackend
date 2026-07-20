# Task: Make the backend's agent-facing API match the real Windows agent's contract

## Context
This is a follow-up task on `Al-AmeenBackend/DLPManagementSystem` (ASP.NET Core 8 / EF Core / SQL Server). A
previous pass already added JWT auth for human admin users and full CRUD for Users/Devices/Employees/Alerts plus
the Permission Requests/Grants workflow and lookups — that part is done and working, **do not touch it**
(`Controllers/AuthController.cs`, `UsersController.cs`, `DevicesController.cs`, `EmployeesController.cs`,
`AlertsController.cs`, `PermissionRequestController.cs`, `PermissionGrantsController.cs`,
`LookupsController.cs`, and their services/DTOs are all out of scope here).

This task is only about the 4 **agent-facing** controllers: `AgentEnrollmentController`,
`AgentAuditEventsController`, `AgentHeartbeatController`, `AgentPolicyController`, plus 3 endpoints that don't
exist yet. These are called by a separate, already-built, production-grade Windows Service agent
(`win-form/Al-Ameen-windows/src/CompanyDlp.Service`) — a mature codebase with its own OpenAPI contract, unit
tests, and deployment scripts. **That agent is the source of truth.** It was built against a formal contract; the
backend was built independently and now needs to be reconciled to match it — not the other way around. Do not
change routes, field names, or the response shape described below; if you can't match it exactly, stop and leave
a clear comment explaining why instead of inventing a different shape.

If you have read access to `win-form/Al-Ameen-windows` from where you're running, the two ground-truth files are:
- `win-form/Al-Ameen-windows/contracts/company-dlp-agent-api.openapi.yaml`
- `win-form/Al-Ameen-windows/src/CompanyDlp.Contracts/*.cs` (`BackendContracts.cs`, `AuditContracts.cs`, `Messages.cs`)
- `win-form/Al-Ameen-windows/src/CompanyDlp.Service/BackendApiClient.cs` and `BackendRequestAuthenticator.cs`

Everything you need from them is already extracted below, so proceed even if you don't have access to that folder.

## What's wrong today (verified by reading both codebases)

| Endpoint | Backend today | Agent expects |
|---|---|---|
| Enroll | `POST api/agent/enroll`, body `{enrollmentToken, machineName, machineSid?, operatingSystem, osVersion?, serialNumber?, macAddress?, agentVersion}`, returns `{deviceKey, agentSecret, enrolledAtUtc}` | `POST api/v1/agent/enroll`, body `{tenantId, deviceId, machineName, agentVersion, enrollmentCode}`, returns `{accessToken, expiresAtUtc}` |
| Heartbeat | `POST api/v1/agent/heartbeat` (route already matches), auth via `X-Device-Key`/`X-Agent-Secret` headers, body `{policyVersion?, currentPolicyVersion?, policyHash?, status?}` | Same route, auth via `Authorization: Bearer <deviceAccessToken>`, body `{tenantId, deviceId, machineName, agentVersion, osVersion, sentAtUtc, lastAppliedPolicyVersion, pendingAuditEventCount}`, returns `{serverTimeUtc, policyRefreshRequired}` |
| Audit events | `POST api/agent/audit-events`, auth via `X-Device-Key`/`X-Agent-Secret` headers, body `{deviceKey, agentVersion, policyVersion, batchId, events:[{correlationId, userSid, username, actionKey, decision, reasonCode, eventType?, occurredAtUtc, metadata}]}` | `POST api/v1/agent/events/batch`, auth via Bearer, body `{tenantId, deviceId, agentVersion, events: SecurityEventEnvelope[]}` (full envelope, see below), returns `{acceptedEventIds, duplicateEventIds, rejectedEvents}` |
| Policy | `GET api/v1/agent/policy` (route already matches, response shape already close), auth via `X-Device-Key`/`X-Agent-Secret` headers | Same route/response, auth via Bearer |
| File classification | doesn't exist | `POST api/v1/agent/file-classification` |
| File key wrap | doesn't exist | `POST api/v1/agent/file-keys/wrap` |
| File key unwrap | doesn't exist | `POST api/v1/agent/file-keys/unwrap` |

The single biggest structural change is **authentication**: replace the `X-Device-Key`/`X-Agent-Secret` header
pair with a proper opaque bearer device token, issued at enrollment, on every one of these 5 endpoints (enroll
itself is anonymous). Everything else is a DTO/route reshape.

## Useful building blocks already in the codebase — reuse, don't recreate
- `Models/AgentEnrollmentToken.cs` (`TokenHash`, `OrganizationId`, `ExpiresAtUtc`, `MaxUses`, `UsedCount`,
  `RevokedAtUtc`) is exactly the "enrollment code" the agent's `enrollmentCode` field refers to — the current
  `AgentEnrollmentService.Enroll` already looks this up correctly by hash. Keep that part.
- `Models/DeviceCredential.cs` (`DeviceId`, `SecretHash`, `CreatedAtUtc`, `LastUsedAtUtc`, `RevokedAtUtc`,
  `RotationDueAtUtc`) is exactly the right shape to store the hash of the new opaque device access token. Reuse
  it as the device's bearer-token store instead of an "agent secret" — hash the issued token the same way
  (`Helper/Hashing/SecurityHashHelper.Sha256`) and store it in `SecretHash`; use `RotationDueAtUtc` as the
  token's `expiresAtUtc` (or add a dedicated `ExpiresAtUtc` column via a new migration if you'd rather not
  overload that field's meaning — your call, just be consistent).
- `Common/ApiResponse<T>` wrapper: **do not use it on these 5 endpoints.** The agent's `BackendApiClient.cs`
  deserializes the raw response body directly into the DTOs shown below (no `{success, data}` envelope) — that's
  a hard requirement of the OpenAPI contract, not a style choice. `AgentPolicyController` already does this
  correctly (`return Ok(response.Data.Snapshot)`, not `Ok(response)`) — copy that pattern everywhere in this task.

## Tasks

### 1. Device bearer authentication
Add a second ASP.NET Core authentication scheme (separate from the existing JWT scheme used for human admin
users) — e.g. a custom `AuthenticationHandler<AuthenticationSchemeOptions>` named `DeviceBearer` that:
- Reads the `Authorization: Bearer <token>` header.
- Hashes the token and looks up a non-revoked, non-expired `DeviceCredential` by `SecretHash`.
- On success, builds a `ClaimsPrincipal` with the resolved `DeviceId` and the device's `OrganizationId` (via
  `DeviceCredential.Device.OrganizationId`) as claims, and updates `LastUsedAtUtc`.
- Register it in `Program.cs` alongside the existing JWT scheme (`AddAuthentication(...).AddJwtBearer(...)`) via
  `.AddScheme<AuthenticationSchemeOptions, DeviceBearerAuthenticationHandler>("DeviceBearer", null)`.
- Apply `[Authorize(AuthenticationSchemes = "DeviceBearer")]` to `AgentAuditEventsController`,
  `AgentHeartbeatController`, `AgentPolicyController`, and the 3 new controllers from task 4. Leave
  `AgentEnrollmentController` anonymous (that's how a device gets its first token).

### 2. Rewrite `AgentEnrollmentController` / `AgentEnrollmentService`
- Route: `POST api/v1/agent/enroll` (add the missing `v1`).
- Request DTO: `{ tenantId: Guid, deviceId: Guid, machineName: string, agentVersion: string, enrollmentCode: string }`.
  `enrollmentCode` replaces `EnrollmentToken` (same lookup logic against `AgentEnrollmentTokens` by hash).
  Validate `tenantId` matches the resolved enrollment token's `OrganizationId` — reject with 400 if not.
- Use the client-supplied `deviceId` as the new `Device.Id` (primary key) instead of generating a new Guid —
  the agent generates and persists its own device id locally and expects the server to accept it.
- Generate one opaque access token (e.g. `SecurityHashHelper.GenerateSecret()`, already exists), hash it, store
  it as a new `DeviceCredential` row for that device, revoke any previous credentials for the same device.
- Response DTO: `{ accessToken: string, expiresAtUtc: DateTimeOffset }` — return the raw token once, never store
  it in plaintext.
- Keep the existing "device already enrolled" / "invalid or expired code" / "usage limit reached" checks, just
  adapt them to the new field names.

### 3. Rewrite `AgentHeartbeatController` / `AgentHeartbeatService` and `AgentPolicyController` / `AgentPolicyService`
- Heartbeat request DTO: `{ tenantId, deviceId, machineName, agentVersion, osVersion?, sentAtUtc, lastAppliedPolicyVersion: long, pendingAuditEventCount: int }`.
  Update `Device.LastSeenAtUtc`, `Device.AgentVersion`, and compare `lastAppliedPolicyVersion` against the
  device's current policy version to decide the response.
- Heartbeat response DTO: `{ serverTimeUtc: DateTimeOffset, policyRefreshRequired: bool }` — no wrapper, no
  device info echoed back.
- Policy endpoint: keep the route and query params (`tenantId`, `deviceId`, `currentVersion`) as-is — they
  already match. Just switch its auth to `DeviceBearer` and remove the header-based lookup
  (`Request.Headers["X-Device-Key"]` etc.) in favor of reading the resolved device from the authenticated
  principal. Keep the existing 200-with-raw-snapshot / 204-no-update behavior.

### 4. Rewrite `AgentAuditEventsController` / `AgentAuditEventService`
- Route: `POST api/v1/agent/events/batch` (was `api/agent/audit-events`).
- Request DTO: `{ tenantId, deviceId, agentVersion, events: SecurityEventEnvelope[] }` where each envelope is:
  ```
  eventId (guid), correlationId (guid), protocolVersion ("1.0"), eventSchemaVersion ("1.0"),
  tenantId (guid), deviceId (guid), userId (guid?), userSid, username, machineName,
  windowsSessionId (int?), actionKey, eventType, decision ("Allow"|"Block"|"Audit"|"Error"),
  reasonCode, policyId (guid?), policyVersion (long?), ruleId, permissionGrantId (guid?),
  sourceProcess (object?), resource (object?), destination (object?), details (object),
  occurredAtUtc, agentVersion, osVersion, isDevelopmentEvent (bool), integrityHash (64-char hex)
  ```
  Store `sourceProcess`/`resource`/`destination`/`details` as JSON columns/`JsonElement` — they're free-form.
  Map this into the existing `AuditEvent`/`ObservedFile` tables where the fields line up; anything without a
  home in the current schema can go into a JSON metadata column on `AuditEvent` (add one via migration if there
  isn't one already — check first).
- Response DTO: `{ acceptedEventIds: Guid[], duplicateEventIds: Guid[], rejectedEvents: [{eventId, reasonCode, retryable}] }`.
  Treat `eventId` as the idempotency key: if an `AuditEvent` with that id already exists, put it in
  `duplicateEventIds` instead of inserting again or erroring.

### 5. New: file classification + file key wrap/unwrap (MVP scope — keep these simple)
Three new endpoints, all `[Authorize(AuthenticationSchemes = "DeviceBearer")]`:
- `POST api/v1/agent/file-classification` — request
  `{ requestId, tenantId, deviceId, userSid?, fileName, extension?, sizeBytes, mimeType?, sha256?, channel, destination, requestedAtUtc }`,
  response `{ requestId, isAllowed: bool, isSensitive: bool, classification: string, reasonCode: string, provider: string, ruleId?: string, evaluatedAtUtc, validUntilUtc? }`.
  For MVP, implement a simple rule pass (e.g. default-allow unless the file matches an obviously blocked
  extension/pattern from the org's policy) — this doesn't need to be a real content-inspection engine yet, just
  return a well-formed response so the agent's pipeline isn't blocked. Leave a `// TODO` that real classification
  logic (matching the `SensitiveRules` already modeled in `AgentPolicyController`'s policy snapshot) can replace
  the stub later.
- `POST api/v1/agent/file-keys/wrap` — request `{ tenantId, deviceId, fileId, plainKeyBase64 }`, response
  `{ keyId, wrappedKeyBase64 }`. Implement using ASP.NET Core's built-in Data Protection API
  (`IDataProtectionProvider` → `CreateProtector("CompanyDlp.FileKeys")` → `.Protect(bytes)`), which is exactly an
  envelope-encryption wrap/unwrap primitive with key rotation handled for you — no need to build or manage a KMS.
  Generate `keyId` as a new Guid.
- `POST api/v1/agent/file-keys/unwrap` — request `{ tenantId, deviceId, fileId, keyId, wrappedKeyBase64 }`,
  response `{ plainKeyBase64 }`, using the same protector's `.Unprotect(bytes)`.
  **Never log the plaintext key** in either direction — this is a "writeOnly" field per the contract.

## Acceptance criteria
- `dotnet build` succeeds with zero errors.
- The admin-facing controllers from the previous task are untouched and still work.
- All 5 device-authenticated endpoints reject requests without a valid `Authorization: Bearer` device token
  (401), and accept one issued by the new enroll flow.
- Every request/response shape matches the tables and schemas above field-for-field (names and casing — ASP.NET
  Core's default camelCase JSON serialization should already line up with the contract, but double-check).
- `POST api/v1/agent/enroll` → `POST api/v1/agent/heartbeat` → `GET api/v1/agent/policy` →
  `POST api/v1/agent/events/batch` works end-to-end in a manual test using the token from step 1.
