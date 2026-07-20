# Task: Implement Auth + Admin API for the DLP Management System backend

## Where to work
Project root: `Al-AmeenBackend/DLPManagementSystem` (ASP.NET Core 8 Web API, EF Core 8, SQL Server).
Solution file: `Al-AmeenBackend/DLPManagementSystem.sln`.

## Current state (read this before touching anything)
- `Models/DLPSystemContext.cs` already has ~50 DbSets fully mapped (Organization, User, Role, Employee,
  Department, Device, AuditEvent, AiAnalysisResult, AiAnalysisOverride, Alert, AlertLevel, AlertStatus,
  PermissionRequest, PermissionGrant, PermissionAction, PolicyVersion, AgentEnrollmentToken, DeviceHeartbeat,
  UsbDeviceInventory, SoftwareInventory, etc.). This schema is solid and multi-tenant (almost every table has
  `OrganizationId`). **Do not redesign the schema.** Only add columns/tables if strictly required (e.g. a
  refresh-token store), and do it via a new EF Core migration, never by hand-editing the existing migration.
- Only 5 controllers exist today, all under `Controllers/` and `CompanyDlpDashboard/`:
  `AgentAuditEventsController`, `AgentEnrollmentController`, `AgentHeartbeatController`, `AgentPolicyController`
  (agent-facing, authenticated via `X-Device-Key`/`X-Agent-Secret` headers — **do not change their auth model**),
  and `DlpDashboardController` (`GET /api/v1/dashboard/summary`, already working, consumed by the frontend).
- `Service/Interface/IAuthService.cs` is an **empty interface**, never implemented, never registered in DI, no
  `AuthController` exists. `appsettings.json` already has a `Jwt` section (Issuer/Audience/SecretKey/AccessTokenMinutes)
  waiting to be used — nobody reads it yet.
- `Service/Interface/IPermissionLookupService.cs` + its implementation exist and are registered in DI but are
  currently unused (no controller calls them).
- `DTO/Permissions/Contracts/` already has finished DTOs for the permission-request workflow:
  `CreatePermissionRequestDto`, `PermissionRequestDto`, `PermissionActionDto`, `PermissionGrantDto`,
  `ReviewPermissionRequestDto`. Reuse these as-is, don't rewrite them.
- `Common/ApiResponse.cs` defines the response envelope used by the existing controllers — **every new endpoint
  must return this**:
  ```csharp
  public class ApiResponse<T> {
      public bool Success { get; set; }
      public string MessageAr { get; set; }
      public string MessageEn { get; set; }
      public T? Data { get; set; }
  }
  ```
- Existing pattern to follow for every feature: `Service/Interface/I<Name>Service.cs` +
  `Service/Service/<Name>Service.cs`, registered in `Program.cs` via `AddScoped<I...,...>()`, DTOs under
  `DTO/<Feature>/...`.
- `Program.cs` already configures CORS for `http://localhost:4200` (the Angular dev server) — keep that policy.
- `Data/Seed/DatabaseSeeder.cs` seeds roles (SuperAdmin, SecurityAdmin, HelpDesk, Auditor, Employee), user types,
  statuses, and one dev admin user for testing:
  `Email = dev.admin@companydlp.local`, `Password = DevAdmin123!`, hashed via
  `Helper/Hashing/SecurityHashHelper.Sha256(...)` (plain salted-less SHA-256 — keep using this helper for now so
  the seeded user keeps working; leave a `// TODO` that this should move to a proper salted hash like
  `BCrypt.Net` or `Microsoft.AspNetCore.Identity.PasswordHasher<T>` later — don't do the migration now, just flag it).
- There is **no git history** in the repo yet (`git status` shows "No commits yet"). There are stray `.bak` files
  in `CompanyDlpDashboard/` (`*.before-*.bak`, `*.corrupted.bak`) — delete them, they're dead weight.

## Goal
Add JWT authentication and the admin-facing CRUD/workflow endpoints the schema already supports, so a separate
Angular frontend (being built independently against the contract below) has a real API to call.

## Tasks

1. **Auth**
   - Add the `Microsoft.AspNetCore.Authentication.JwtBearer` NuGet package (match the `8.0.x` version already used
     by the other `Microsoft.EntityFrameworkCore.*` packages).
   - Implement `IAuthService`/`AuthService`: `Login(email, password)` looks up the `User` by `Email`, compares
     `PasswordHash` via `SecurityHashHelper.Sha256`, checks the linked `UserStatus` is the "Active" one, then
     issues a JWT using the existing `Jwt` config section. Claims must include: `sub` = User.Id,
     `email`, `role` = Role.Name, `roleId`, `organizationId`, `userTypeId`.
   - Wire `builder.Services.AddAuthentication(...).AddJwtBearer(...)` in `Program.cs`, reading the same `Jwt`
     section, and call `app.UseAuthentication()` **before** `app.UseAuthorization()`.
   - New `Controllers/AuthController.cs`: `POST /api/v1/auth/login`, `GET /api/v1/auth/me` (see contract below).
   - Put `[Authorize]` on every new controller from this task. Leave the 4 agent controllers untouched. Add
     `[Authorize]` to `DlpDashboardController` too (the frontend will start sending a Bearer token once its login
     flow lands — that's being built in parallel, so this is expected to "break" the anonymous dashboard call
     until both sides are wired together — that's fine).

2. **Core admin CRUD** — implement using the Service/Interface + Service/Service + DTO pattern already in the
   codebase, all scoped to the caller's `OrganizationId` from the JWT claim (never trust a client-supplied org id):
   Users, Devices, Employees, Alerts. Exact routes/DTO shapes are in the **API contract** section below — follow
   it exactly, field names and all, since the frontend agent is coding against the same contract without seeing
   your code.

3. **Permission-request workflow** — build `PermissionRequestController` + `PermissionRequestService` using the
   DTOs that already exist in `DTO/Permissions/Contracts/`. Approve should create a `PermissionGrant` row; reject
   should just update status/review fields. Use `IPermissionLookupService` (already implemented) to resolve the
   lookup ids instead of hardcoding them.

4. **Lookups** — a small `LookupsController` exposing the reference tables the frontend needs for dropdowns
   (roles, alert levels/statuses, device statuses, employee statuses, departments, permission actions).

5. **Cleanup** — delete the `*.bak` files under `CompanyDlpDashboard/`. Run `git init` if needed and make a
   real initial commit once the build is green.

## Shared API contract (must match the frontend exactly — do not rename fields)

Base URL in dev: `https://localhost:7008`. Every response is `ApiResponse<T>` as shown above. Every list endpoint
returns `data` shaped as:
```json
{ "items": [...], "totalCount": 0, "page": 1, "pageSize": 20 }
```
Auth header on every endpoint except `/auth/login`: `Authorization: Bearer <accessToken>`.

**Auth**
- `POST /api/v1/auth/login` — body `{ email, password }` → data:
  `{ accessToken, expiresAtUtc, user: { id, fullName, email, roleId, roleName, organizationId, userTypeId } }`
- `GET /api/v1/auth/me` → data: same `user` shape as above.

**Lookups**
- `GET /api/v1/lookups/roles` → `{ id, name, displayName }[]`
- `GET /api/v1/lookups/alert-levels`, `/alert-statuses`, `/device-statuses`, `/employee-statuses`,
  `/user-statuses`, `/departments` → `{ id, name }[]`
- `GET /api/v1/permission-actions` → `PermissionActionDto[]` (existing DTO)

**Users** (`/api/v1/users`)
- `GET ?search=&roleId=&statusId=&page=1&pageSize=20` → paged `{ id, fullName, email, roleId, roleName, statusId, statusName, lastLoginAtUtc, createdAtUtc }`
- `GET /{id}` → detail (adds linked employee id/name if any)
- `POST` — body `{ fullName, email, password, roleId, userTypeId }`
- `PUT /{id}` — body `{ fullName, roleId, statusId }`
- `POST /{id}/reset-password` — body `{ newPassword }`
- `DELETE /{id}` — soft-delete (sets an inactive status, don't hard-delete)

**Devices** (`/api/v1/devices`)
- `GET ?search=&statusId=&page=&pageSize=` → paged `{ id, machineName, operatingSystem, statusId, statusName, lastSeenAtUtc, currentPolicyVersion, assignedEmployeeName }`
- `GET /{id}` → detail
- `PUT /{id}` — body `{ machineName, statusId }`
- `DELETE /{id}` — soft-delete/decommission
- `POST /{id}/assign` — body `{ employeeId }`
- `POST /{id}/unassign`

**Employees** (`/api/v1/employees`)
- `GET ?search=&departmentId=&statusId=&page=&pageSize=` → paged list
- `GET /{id}`, `POST`, `PUT /{id}`, `DELETE /{id}` — standard CRUD, fields: `departmentId, employeeNumber, displayName, email, phoneNumber, statusId`

**Alerts** (`/api/v1/alerts`)
- `GET ?statusId=&levelId=&assignedToUserId=&fromUtc=&toUtc=&page=&pageSize=` → paged
  `{ id, title, alertLevelId, alertLevelName, alertStatusId, alertStatusName, assignedToUserId, assignedToUserName, createdAtUtc, closedAtUtc, isFalsePositive }`
- `GET /{id}` → detail, includes the linked audit event summary (device name, employee name, action, occurred time, AI decision/risk score if present)
- `PUT /{id}/assign` — body `{ assignedToUserId }`
- `PUT /{id}/status` — body `{ alertStatusId, investigationNotes?, isFalsePositive? }` (server sets `ClosedAtUtc` when moved to Closed)

**Permission requests** (`/api/v1/permission-requests`, reuse existing DTOs)
- `GET ?statusId=&requestedByEmployeeId=&page=&pageSize=` → paged `PermissionRequestDto`
- `GET /{id}` → `PermissionRequestDto`
- `POST` — body `CreatePermissionRequestDto`
- `POST /{id}/approve` — body `ReviewPermissionRequestDto`
- `POST /{id}/reject` — body `ReviewPermissionRequestDto`

**Permission grants** (`/api/v1/permission-grants`)
- `GET ?subjectId=&actionKey=&page=&pageSize=` → paged `PermissionGrantDto`
- `POST /{id}/revoke` — body `{ revocationReason }`

**Dashboard** — already implemented, don't change the shape: `GET /api/v1/dashboard/summary?fromUtc=&toUtc=`.

## Acceptance criteria
- `dotnet build` succeeds with zero errors from the solution root.
- Swagger (`/swagger`) lists every new endpoint.
- Logging in with `dev.admin@companydlp.local` / `DevAdmin123!` returns a working JWT, and that token
  successfully authorizes a call to `GET /api/v1/users`.
- The 4 existing agent controllers behave exactly as before (still no JWT required on them).
- All new queries are scoped by the caller's `OrganizationId` — verify by checking the generated SQL or adding a
  quick test with two orgs.
- No hand-edits to the existing `20260720100140_InitialCreate` migration; any new schema need goes through
  `dotnet ef migrations add <Name>`.
