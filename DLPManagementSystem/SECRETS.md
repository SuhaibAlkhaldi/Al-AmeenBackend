# Local secrets

Only `FileClassificationApi:ApiKey` is a real secret — `appsettings.json` intentionally leaves it blank and it
must never contain a real value in source control. Set it locally with the .NET user-secrets store
(per-developer, lives outside the repo):

```
cd DLPManagementSystem
dotnet user-secrets set "FileClassificationApi:ApiKey" "<your-api-key>"
```

In production, supply it via an environment variable (`FileClassificationApi__ApiKey`) or your hosting
platform's secret store — not this file.

`ConnectionStrings:DefaultConnection` is **not** a secret (it uses `Trusted_Connection=True` — Windows
Integrated Authentication, no password) and has a safe committed default in `appsettings.json` that works
out of the box for anyone using a local `127.0.0.1,1433` SQL Server instance. If your local SQL Server setup
differs, override it locally via `appsettings.Development.json` or `dotnet user-secrets set
"ConnectionStrings:DefaultConnection" "..."` — don't edit the committed default for your own machine.
