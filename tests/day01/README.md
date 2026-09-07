# Day 1 integration checks

These tests exercise the actual API, EF Core SQL Server provider and SQL Server database.
They never target the configured LCAP_HRMS database.

## Existing suites

From the project root:

```powershell
dotnet test backend/LCAP.HRMS.sln --configuration Release
Push-Location frontend/lcap-hrms-web
npm test
npm run build
Pop-Location
```

## SQL Server and browser integration

Use a fresh local database for each run. The SQL scripts and harness require the prefix LCAP_HRMS_Day01_.
Use Windows authentication. Do not substitute a production database or server.

1. Generate the current idempotent script:

```powershell
dotnet ef migrations script 0 ShiftMaster --project backend/LCAP.HRMS.Infrastructure --startup-project backend/LCAP.HRMS.Api --configuration Release --output artifacts/day01/validated-migrations.sql --idempotent
```

2. Choose a unique database name, for example LCAP_HRMS_Day01_YYYYMMDD_RANDOM. Run a guarded IF DB_ID(...) IS NULL CREATE DATABASE statement twice using sqlcmd against localhost. Apply the generated script with sqlcmd -b -E -C -S localhost -d YOUR_TEST_DATABASE -i artifacts/day01/validated-migrations.sql.
3. Run verify-seeds.sql against that database. Apply the migration script again and run verify-seeds.sql again. Both must pass.
4. Build and start the test host:

```powershell
dotnet build tests/LCAP.HRMS.Day01Harness --configuration Release
dotnet tests/LCAP.HRMS.Day01Harness/bin/Release/net8.0/LCAP.HRMS.Day01Harness.dll "ABSOLUTE_PROJECT_ROOT" "YOUR_TEST_DATABASE"
```

The host binds only to http://127.0.0.1:5091. It serves the built Angular frontend and forwards API requests to the actual ASP.NET application hosted by WebApplicationFactory. DbContext is configured for the guarded test database; the host verifies the effective database name before serving requests.

The real JWT bearer middleware validates a freshly generated one-hour token using an ephemeral test signing key and local test issuer configuration. Production JWT configuration is untouched. External identity-provider discovery/rotation is not exercised.

5. In another terminal at the project root:

```powershell
node --test tests/day01/api-integration.test.mjs
sqlcmd -S localhost -E -C -b -d YOUR_TEST_DATABASE -i tests/day01/verify-constraints.sql
```

The HTTP tests use artifacts/day01/access-token.txt and write http-evidence.json. Scripts expect a freshly migrated database; CRUD tests reserve their codes through soft-delete.

6. Open http://127.0.0.1:5091 in the browser, use Profile → API connection and supply the generated test token. Check all six screens and a disposable UI-created record. No fake statutory identifiers or coordinates are needed.
7. Stop the host, remove artifacts/day01/access-token.txt, and retain or drop only the isolated test database according to your local test-data policy.

The SQL verification scripts assert seed counts, null statutory identifiers/coordinates, unique indexes, retained soft-deleted audit rows and a SQL check constraint. Artifacts and test results are ignored by version control.
