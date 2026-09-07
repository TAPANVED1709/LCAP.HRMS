# Day 4 integration checks

Run from the repository root with .NET 8/EF tools, Node/npm, SQL Server 2022 on localhost and SQLCMD. Use a **new** `LCAP_HRMS_Day04_*` database for each complete scenario run. Never point the harness at production or previous-day data.

```powershell
New-Item -ItemType Directory -Force artifacts/day04 | Out-Null
dotnet restore backend/LCAP.HRMS.sln
dotnet build backend/LCAP.HRMS.sln -c Release -m:1
dotnet test backend/LCAP.HRMS.sln -c Release --no-build -m:1
dotnet ef migrations has-pending-model-changes --project backend/LCAP.HRMS.Infrastructure --startup-project backend/LCAP.HRMS.Api --configuration Release --no-build
dotnet ef migrations script --idempotent --project backend/LCAP.HRMS.Infrastructure --startup-project backend/LCAP.HRMS.Api --configuration Release --no-build --output artifacts/day04/migrations.sql
```

In `frontend/lcap-hrms-web`, run `npm ci`, `npm test`, and `npm run build -- --configuration production`. Return to the repository root. Apply migrations **sequentially**, waiting for each command to exit. `-I` enables QUOTED_IDENTIFIER for filtered indexes.

```powershell
sqlcmd -S localhost -E -C -I -b -Q "CREATE DATABASE LCAP_HRMS_Day04_YourUniqueRun;"
sqlcmd -S localhost -E -C -I -b -d LCAP_HRMS_Day04_YourUniqueRun -i artifacts/day04/migrations.sql
sqlcmd -S localhost -E -C -I -b -d LCAP_HRMS_Day04_YourUniqueRun -i artifacts/day04/migrations.sql
dotnet build tests/LCAP.HRMS.Day04Harness -c Release -m:1
dotnet tests/LCAP.HRMS.Day04Harness/bin/Release/net8.0/LCAP.HRMS.Day04Harness.dll "C:\path\to\LCAP.HRMS" LCAP_HRMS_Day04_YourUniqueRun
```

Run the harness in a dedicated terminal, or use a hidden background process. Wait for its ready message. It binds only to `127.0.0.1:5094`, validates real signed JWTs using an ephemeral key, uses integrated SQL security, and serves the production Angular assets. It does not migrate the database. Its `/test/token`, `/test/clock`, `/test/time`, `/test/evaluate/{id}` and synthetic browser GPS route are test-only. Never deploy this separate harness or expose its port publicly. TrustServerCertificate is only used for local test SQL.

In another terminal:

```powershell
$env:DAY04_DB = 'LCAP_HRMS_Day04_YourUniqueRun'
node --test tests/day04/api-integration.test.mjs
sqlcmd -S localhost -E -C -I -b -d LCAP_HRMS_Day04_YourUniqueRun -i tests/day04/verify-database.sql
```

The suite creates TEST-* fixtures, advances an isolated TimeProvider, and tests the five-day LCAP sequence, real authorization, eight simultaneous fresh evaluations, immutable history and concurrent default-policy overlap rejection. Direct raw fixture insertion for concurrency is restricted to the named test database. Repeating the entire fixture suite against the same database will hit code uniqueness; use a fresh database.

## Browser audit

Open `http://127.0.0.1:5094/test/attendance`. The test-only page installs synthetic 0,0 GPS with 10m accuracy and the test clock timestamp. Production source has no override. The router initially shows Organisation.

1. Use Profile → API connection with ignored `artifacts/day04/hr-token.txt`. Open Attendance → Attendance policies. Verify required validation, create a **nondefault TEST-* policy**, edit, read-only view, deactivate/reactivate and feedback.
2. Connect `browser-token.txt` and open Attendance. The suite leaves this employee at three lates on 10 September, without an event. Verify 15-minute grace, 3-minute lateness, 3-of-3 warning and self-only history.
3. POST `/test/clock` with JSON `{"utc":"2026-09-11T04:20:00Z"}`. Refresh, Check In, then Check Out. Expect five minutes late, historical count4, generated-event message and current count0.
4. Reconnect HR. Verify a Pending event for TEST-DAY04-BROWSER in `/attendance/penalties`, date-filter validation/recovery, and HR register filters. No money/consume/edit controls should exist.
5. Inspect browser console and API logs; save safe screenshots. Re-run SQL verification.

Tokens expire after one hour and become invalid on harness restart. `/test/token` accepts role/companyId/employeeId to refresh them within the isolated host; never print or commit token values. Stop only the owned host and remove temporary token files after verification. Keep safe evidence/database for inspection; no automatic database deletion is provided.
