# Local secrets

## STOP - if you're reading this because the server won't start at all (Jwt:SecretKey error)
`Program.cs` now refuses to start in the Production environment if `Jwt:SecretKey` is still the
committed placeholder or shorter than 32 characters. This is intentional - the committed value in
`appsettings.json` (`CHANGE_THIS_TO_A_LONG_SECURE_SECRET_KEY_32_CHARS_MINIMUM`) must never be what
actually signs tokens in production, because anyone who reads this repo could forge a valid
`SuperAdmin` bearer token with it. Generate a real, unique secret (at least 32 random characters -
e.g. `openssl rand -base64 48`) and set it via an environment variable, the same pattern as the
connection string below:
```
Jwt__SecretKey=<the real, random, 32+ character secret>
```
Set this wherever the backend process actually starts on the server, then restart it. If you rotate
this value later, every existing bearer token becomes invalid immediately (all users/devices have to
log in / re-enroll again) - so treat it as a real secret, but don't be afraid to rotate it once, now,
if there's any chance the placeholder was ever actually live.

## STOP - if you're reading this because the server won't connect to the database
This file was just restored to its safe committed default
(`Server=127.0.0.1,1433;...Trusted_Connection=True...`) after a real production SQL credential
(`Server=161.97.90.171,8797;...User Id=sa;Password=Admin@Password@161@!...`) was found committed directly in
`appsettings.json` — a live plaintext `sa` password for the real production database, exposed in git history.
**That password must be rotated on the actual SQL Server** (this repo alone can't do that). Until then, anyone
with read access to this repo or its git history can still use the old value.

The real production connection string must be supplied to the running server via an environment variable —
**not committed to any appsettings file again**:
```
ConnectionStrings__DefaultConnection=Server=161.97.90.171,8797;Database=DLPSystem;User Id=sa;Password=<the-rotated-password>;TrustServerCertificate=True
```
Set this in whatever actually starts the backend process on the server (a systemd unit's `Environment=`, an
IIS site's environment variables, a supervisor/pm2 config, etc. — check how the process is actually being
started there) before restarting with this updated code, or the app will fall back to the safe local default
above and fail to reach the real database.

## STOP - cleanup needed on the real production database, not just this repo
The code fix above only stops `dev.admin@companydlp.local` / `test.employee@companydlp.local` / the
`DEV-ENROLLMENT-TOKEN` enrollment token from being (re-)created going forward. It does **not** remove
them if a previous startup already created them in the real production database - which is very
likely, since this seeding ran unconditionally on every startup until now. Do this on the real
server, in this order:

1. **First, make sure you have a real admin account that isn't `dev.admin`.** If `dev.admin@companydlp.local`
   is the only account you currently log in with, log in with it one more time, go to the **Users**
   page, and create a brand-new `SuperAdmin` user with a strong unique password. Log out and confirm
   you can log back in with the new account before doing anything else below - don't lock yourself out.
2. Back in **Users**, find `dev.admin@companydlp.local` and `test.employee@companydlp.local` and
   either disable or delete both.
3. Go to **Device Enrollment Tokens** (or query the `AgentEnrollmentTokens` table directly) and revoke
   any token named "Development Enrollment Token" / whose plaintext was `DEV-ENROLLMENT-TOKEN`. If
   any devices were enrolled through it that you don't recognize, check the **Devices** page for
   unexpected entries.
4. Redeploy this backend with the code changes in this update (the seeding guard + the Swagger fix +
   the JWT fail-closed check) so this can't silently happen again.

## FileClassificationApi:ApiKey
A real secret — `appsettings.json` intentionally leaves it blank and it must never contain a real value in
source control. Set it locally with the .NET user-secrets store (per-developer, lives outside the repo):

```
cd DLPManagementSystem
dotnet user-secrets set "FileClassificationApi:ApiKey" "<your-api-key>"
```

In production, supply it via an environment variable (`FileClassificationApi__ApiKey`) or your hosting
platform's secret store — not this file. (Note: as of this writing the real key is still sitting in the
committed `appsettings.json` at the explicit request of the project owner, to keep a live AI-classification
verification task working — this is a known, deliberate exception, not an oversight. It should still move to
user-secrets/an env var and get rotated before this is genuinely production-hardened.)

## ConnectionStrings:DefaultConnection
Not a secret **in its committed form** — `appsettings.json`'s default uses `Trusted_Connection=True` (Windows
Integrated Authentication, no password) and works out of the box for anyone using a local `127.0.0.1,1433` SQL
Server instance. The *real* production value (above) is a genuine secret and must only ever live in an
environment variable on the actual server, never in a committed file. If your local SQL Server setup differs
from the default, override it locally via `appsettings.Development.json` or `dotnet user-secrets set
"ConnectionStrings:DefaultConnection" "..."` — don't edit the committed default for your own machine.

## Cors:AllowedOrigins
Not a secret, but also environment-specific — `appsettings.json`'s default only allows the local dev server
origin (`http://localhost:4200`). `appsettings.Production.json` carries the real deployed frontend origin. If
that origin ever changes, update `appsettings.Production.json` (or override via the `CORS__AllowedOrigins__0`
environment variable) — don't hardcode a new value directly in `Program.cs` or fall back to `AllowAnyOrigin()`.
