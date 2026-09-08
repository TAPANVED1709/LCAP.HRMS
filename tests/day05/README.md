# Day 5 integration checks

Run from the repository root with .NET 8/EF tools, Node/npm, SQL Server 2022 on localhost and SQLCMD. Use a new `LCAP_HRMS_Day05_*` database for each full fixture run.

```powershell
dotnet restore backend/LCAP.HRMS.sln
dotnet build backend/LCAP.HRMS.sln -c Release -m:1
dotnet test backend/LCAP.HRMS.sln -c Release --no-build -m:1
dotnet ef migrations has-pending-model-changes --project backend/LCAP.HRMS.Infrastructure --startup-project backend/LCAP.HRMS.Api --configuration Release --no-build
New-Item -ItemType Directory -Force artifacts/day05 | Out-Null
dotnet ef migrations script --idempotent --project backend/LCAP.HRMS.Infrastructure --startup-project backend/LCAP.HRMS.Api --configuration Release --no-build --output artifacts/day05/migrations.sql
```

In `frontend/lcap-hrms-web`, run `npm ci`, `npm ls --depth=0`, `npm test`, and `npm run build -- --configuration production`. Return to the repository root. Apply migrations sequentially; `-I` enables QUOTED_IDENTIFIER for filtered indexes.

```powershell
sqlcmd -S localhost -E -C -I -b -Q "CREATE DATABASE LCAP_HRMS_Day05_YourUniqueRun;"
sqlcmd -S localhost -E -C -I -b -d LCAP_HRMS_Day05_YourUniqueRun -i artifacts/day05/migrations.sql
sqlcmd -S localhost -E -C -I -b -d LCAP_HRMS_Day05_YourUniqueRun -i artifacts/day05/migrations.sql
dotnet build tests/LCAP.HRMS.Day05Harness -c Release -m:1
dotnet tests/LCAP.HRMS.Day05Harness/bin/Release/net8.0/LCAP.HRMS.Day05Harness.dll "C:\path\to\LCAP.HRMS" LCAP_HRMS_Day05_YourUniqueRun
```

Run the harness in a dedicated terminal or hidden background process. Wait for readiness. It binds only to `127.0.0.1:5095`, validates signed JWTs with an ephemeral key, uses integrated SQL security and serves built Angular assets. It does not migrate automatically. Its `/test/*` endpoints are isolated fixture helpers; never deploy this harness. Local certificate trust is test-only.

In another terminal:

```powershell
node --test tests/day05/api-integration.test.mjs
sqlcmd -S localhost -E -C -I -b -d LCAP_HRMS_Day05_YourUniqueRun -i tests/day05/verify-database.sql
```

The seven groups exercise real signed-JWT authorization, correction/evaluation history, duplicate submission and review races, missing attendance, corrected checkout, cancellation, rejection and fixed manager assignment. Fixtures use TEST-DAY05 codes and synthetic coordinates only in the isolated database. Reusing the full suite against the same database causes expected code conflicts.

## Browser audit

Open `http://127.0.0.1:5095/attendance/regularisation/new`. Profile → API connection accepts the ignored browser/manager/HR token files produced by the suite.

1. Connect the employee token. Check blank validation and original/effective preview for 18 September 2026. Submit IncorrectCheckIn at 09:40 Asia/Kolkata with a reason.
2. Connect the manager token. Open `/manager/attendance-approvals`, approve that request and verify Applied feedback. Reject the seeded 19 September request: first verify required remarks, then provide remarks and confirm Rejected.
3. Reconnect employee: verify history, read-only completed states and denial of the manager inbox. Submit an Other request for 17 September, then cancel with a reason.
4. Connect HR and open `/attendance/regularisations/admin`. Verify employee/status/date filters and read-only actions.
5. Inspect browser console and API logs; capture safe screenshots. Re-run SQL checks.

The test clock ends on 20 September 2026. Tokens expire after one hour and are invalid after restart. Refresh through the isolated `/test/token` endpoint with role/companyId/employeeId; never print or commit tokens. Stop only the owned host and remove temporary token files when finished. Retain safe logs and the isolated database for inspection.
