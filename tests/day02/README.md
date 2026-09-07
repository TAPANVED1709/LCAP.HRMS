# Day 2 verification

Run from the repository root. Use .NET 8-compatible SDK, Node/npm, and local SQL Server 2022 with Windows authentication. Never substitute a production database.

```powershell
dotnet restore backend/LCAP.HRMS.sln
dotnet build backend/LCAP.HRMS.sln -c Release -m:1
dotnet test backend/LCAP.HRMS.sln -c Release --no-build -m:1
npm ci --prefix frontend/lcap-hrms-web
npm test --prefix frontend/lcap-hrms-web
npm run build --prefix frontend/lcap-hrms-web
dotnet ef migrations has-pending-model-changes --project backend/LCAP.HRMS.Infrastructure --startup-project backend/LCAP.HRMS.Api --configuration Release --no-build
dotnet ef migrations script --idempotent --project backend/LCAP.HRMS.Infrastructure --startup-project backend/LCAP.HRMS.Api --configuration Release --no-build --output database/scripts/EmployeeMaster.sql
dotnet build tests/LCAP.HRMS.Day02Harness -c Release -m:1
```

Build after generating a migration before using `--no-build`, so the compiled migration snapshot is current.

Create a **fresh** disposable database, for example `LCAP_HRMS_Day02_LocalTest_001`, using a guarded CREATE statement. Apply `database/scripts/EmployeeMaster.sql` with `sqlcmd -S localhost -E -C -b -d DATABASE -i SCRIPT`. Apply it again to verify idempotence: eight migrations, no employee seeds, unchanged Day 1 seed counts. The test host rejects names outside `LCAP_HRMS_Day02_...` and confirms the effective DbContext database.

Start the loopback-only host in a terminal:

```powershell
dotnet tests/LCAP.HRMS.Day02Harness/bin/Release/net8.0/LCAP.HRMS.Day02Harness.dll "$PWD" LCAP_HRMS_Day02_LocalTest_001
```

In a second terminal:

```powershell
node --test tests/day02/api-integration.test.mjs
sqlcmd -S localhost -E -C -b -W -d LCAP_HRMS_Day02_LocalTest_001 -i tests/day02/verify-database.sql
```

The suite creates explicitly synthetic test records through the real API; there are no permanent employee migration seeds. Use a fresh database for a complete suite rerun because deleted employee codes remain reserved. Reapplying schema/seed migrations is idempotent; CRUD assertions intentionally test fresh creation.

The host serves the production Angular bundle at `http://127.0.0.1:5092/employees`, forwarding requests to the actual ASP.NET API and SQL Server. It uses real JwtBearer validation with a random in-memory signing key and static test issuer. `artifacts/day02/access-token.txt` holds a one-hour SuperAdmin test token. Profile → API connection accepts it for manual browser testing.

The harness-only POST `/test/token` issues role test tokens from `{ "role": "Manager", "companyId": "...", "employeeId": "..." }`. It exists only in this local test executable, is never included in the product API, and must never be deployed. No production token signing key is used. Role claim name is `role`; company and employee claim names are `company_id` and `employee_id`.

Stop the host and delete the generated access-token file after testing. Retain or remove only your explicitly named test database according to local policy. HTTP evidence records methods, routes and statuses, without payloads or identifiers such as PAN/Aadhaar/bank values. The SQL fixtures use no real employee information.

Browser acceptance: create an employee with all assignments, choose a manager, save, edit, verify retained values and success feedback, check section buttons preserve form state, inspect the profile, activate/deactivate, use search/filters, then connect as the manager and inspect My Team. Confirm employee list responses have no sensitive keys and browser/API logs contain no sensitive test values.

