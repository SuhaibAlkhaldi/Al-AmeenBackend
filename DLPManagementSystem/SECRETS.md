# Local secrets

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
