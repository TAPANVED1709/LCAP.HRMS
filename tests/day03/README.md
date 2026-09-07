# Day 3 integration checks

Run from the repository root. Requires .NET 8/EF tools, Node/npm, SQL Server 2022 on localhost and SQLCMD. Use only a fresh isolated database named `LCAP_HRMS_Day03_*`; the harness refuses other names. Existing Day 1/2 or production databases are not test targets.

```powershell
New-Item -ItemType Directory -Force artifacts/day03 | Out-Null
dotnet restore backend/LCAP.HRMS.sln
dotnet build backend/LCAP.HRMS.sln --configuration Release
dotnet test backend/LCAP.HRMS.sln --configuration Release --no-build
dotnet ef migrations has-pending-model-changes --project backend/LCAP.HRMS.Infrastructure --startup-project backend/LCAP.HRMS.Api --configuration Release --no-build
dotnet ef migrations script --idempotent --project backend/LCAP.HRMS.Infrastructure --startup-project backend/LCAP.HRMS.Api --configuration Release --no-build --output artifacts/day03/migrations.sql
```

In `frontend/lcap-hrms-web`, run `npm ci`, `npm test` and `npm run build`. Return to the repository root. Create a fresh test database, then apply the script **sequentially**, waiting for each command to exit. `-I` enables QUOTED_IDENTIFIER required by filtered indexes.

```powershell
sqlcmd -S localhost -E -C -I -b -Q "CREATE DATABASE LCAP_HRMS_Day03_YourUniqueRun;"
sqlcmd -S localhost -E -C -I -b -d LCAP_HRMS_Day03_YourUniqueRun -i artifacts/day03/migrations.sql
sqlcmd -S localhost -E -C -I -b -d LCAP_HRMS_Day03_YourUniqueRun -i artifacts/day03/migrations.sql
dotnet build tests/LCAP.HRMS.Day03Harness --configuration Release
```

Start the test harness in a dedicated terminal (or hidden background process), with repository path and isolated database name:

```powershell
dotnet tests/LCAP.HRMS.Day03Harness/bin/Release/net8.0/LCAP.HRMS.Day03Harness.dll "C:\path\to\LCAP.HRMS" LCAP_HRMS_Day03_YourUniqueRun
```

The harness binds only to `127.0.0.1:5093`, generates an ephemeral signing key, configures actual JwtBearer validation and the SQL provider, and serves the Angular production assets. It deliberately exposes a test-only `/test/token` issuer. It does not migrate databases. Never deploy the harness or expose its port publicly. Local test SQL uses integrated security and a trusted test-server certificate override; this is not production certificate guidance.

After the ready message, execute once per fresh database:

```powershell
node --test tests/day03/api-integration.test.mjs
sqlcmd -S localhost -E -C -I -b -d LCAP_HRMS_Day03_YourUniqueRun -i tests/day03/verify-database.sql
```

The API suite creates TEST-* employees/locations, tests real signed JWTs and four concurrent requests, and saves safe HTTP evidence plus a browser fixture token under ignored `artifacts/day03/`. These fixtures are deliberately separate from permanent seeds. Rerunning against the same database will encounter fixture code uniqueness; create another isolated database.

For the browser test, open `http://127.0.0.1:5093/test/attendance`. This test-only HTML route installs a one-shot Geolocation API shim returning synthetic 0,0 with 10m accuracy and a fresh timestamp. The Angular router initially redirects to the organisation page; use Profile → API connection to enter the generated `browser-token.txt`, then choose Attendance. Verify Asia/Kolkata, Not Checked In → Checked In → Completed, safe messages, and one history record. Production Angular source contains no GPS override. Normal browser geolocation requires HTTPS, except permitted loopback development contexts.

The register can be tested with a test HRAdmin token issued by `/test/token` using the company ID from `fixture-ids.json`. It is read-only and supports date/branch/employee filters. Inspect the browser console and API host logs; expected configuration warnings from negative tests are distinct from unhandled failures.

Stop the owned harness when finished and remove the ephemeral token files from `artifacts/day03/`. Retain safe logs/screenshots and isolated databases if needed for inspection; database deletion is not automated.
