# LCAP HRMS - Day 1 Report

## Final Status: PASS

Final completion audit executed on 7 September 2026. PASS applies to the local Day 1 scope, not production deployment certification. No new business features were added. One test-harness defect was fixed and its affected integration tests and builds were rerun successfully.

## Completed

ASP.NET Core .NET 8 API/Application/Domain/Infrastructure separation, Angular layout, SQL Server/EF Core configuration, dependency injection, JWT bearer validation, Swagger, CORS, global exception handling, shared API responses, health endpoint, audit timestamps and soft-delete infrastructure are present and verified.

Environment: .NET SDK 10.0.301 targeting .NET 8; EF Core 8.0.30; Angular 21; Node 24.14.1; npm 11.11.0; SQL Server 2022 Developer 16.0.1000.6, compatibility level 160.

Employee, Attendance, Leave and Payroll features were not implemented during this audit. Their navigation entries remain placeholders.

## Backend Build

**PASS**

- `dotnet restore backend/LCAP.HRMS.sln`: succeeded.
- `dotnet build backend/LCAP.HRMS.sln --configuration Release --no-restore`: succeeded initially and after the harness correction; final run 20.09 seconds, 0 warnings, 0 errors.
- `dotnet build tests/LCAP.HRMS.Day01Harness --configuration Release`: succeeded after correction, 25.78 seconds, 0 warnings, 0 errors.
- `dotnet test backend/LCAP.HRMS.sln --configuration Release --no-build --logger "trx;LogFileName=day01-final.trx"`: 118 passed, 0 failed, 0 skipped (35 persistence tests and 83 API tests).

## Frontend Build

**PASS**

- Initial `npm ci` failed with a Windows EBUSY dependency-directory lock.
- Clean retry `npm ci --prefer-offline --no-audit --no-fund` succeeded: 407 packages installed. This was a dependency installation check, not a vulnerability audit.
- `npm test`: 11 passed, 0 failed.
- `npm run build`: succeeded; rerun after the harness fix also succeeded in 35.857 seconds, without compiler or budget warnings.
- Initial bundle 320.99 kB; Organisation lazy chunk 27.03 kB.

## Database/Migrations

**PASS** against actual local SQL Server 2022 using the EF SQL Server provider and Windows authentication.

- `dotnet ef migrations has-pending-model-changes`: no pending model changes.
- Generated full idempotent SQL: `artifacts/day01/final-audit-migrations.sql`. Its SHA-256 matches the checked-in `database/scripts/ShiftMaster.sql`.
- Guarded database creation executed twice and the migration script applied twice on the first fresh audit database. `verify-seeds.sql` passed after each migration application: seven history entries and seed counts 1/1/5/6/1/1, without duplicates.
- Seven foreign keys are enabled and trusted, all with NO_ACTION deletion. Branch, Department, Designation and Shift belong to Company. Work Location references Branch and Company. Department uses a same-company composite parent relationship.
- SQL duplicate inserts were rejected for all six code constraints. Company codes are globally unique; Branch/Department/Designation/Shift codes are company-scoped; Work Location codes are branch-scoped. Case-insensitive code collation is configured. Soft-deleted codes remain reserved.
- Direct SQL checks confirmed retained soft-deleted rows and audit actors for all six masters. The global filters hide deleted records from normal API reads.
- Latitude and Longitude are nullable decimal(10,7). Seed coordinates are NULL; boundary, paired-coordinate and positive-radius validation passed. Default radius is 100 metres, with geofence enabled.
- Overnight 22:00–06:00 shift CRUD passed. Equal start/end and negative grace were rejected; SQL shift constraints also passed.
- Production SQL configuration uses encryption and certificate verification with a server placeholder. Local certificate trust is limited to development/test settings. No production database was updated or automatically migrated.

Retained isolated databases:

- `LCAP_HRMS_Day01_FinalAudit_20260907_2e6701`: repeated migration/seed checks, initial CRUD suite and browser round trip.
- `LCAP_HRMS_Day01_FinalAudit_20260907_2e6702`: fresh migration and complete CRUD/SQL regression after fixing the harness.

Test-created rows remain soft-deleted; physical row counts therefore exceed initial seed counts. The temporary host was stopped, its browser tab closed and its generated access-token file removed.

## Migrations

1. `20260907081957_InitialFoundation`
2. `20260907082739_CompanyMaster`
3. `20260907083822_BranchMaster`
4. `20260907095757_DepartmentMaster`
5. `20260907100522_DesignationMaster`
6. `20260907101327_WorkLocationMaster`
7. `20260907102112_ShiftMaster`

No migration was added or changed by this audit.

## Masters Completed

| Master | Result | Verified seed |
| --- | --- | --- |
| Company | PASS | LCAP; Bihar; India; INR |
| Branch | PASS | PATNA-HO, Patna Head Office |
| Department | PASS | HR, FIN, SALES, OPS, MGMT |
| Designation | PASS | EXEC, SREXEC, TL, MGR, HRM, PAYADMIN |
| Shift | PASS | GENERAL, 09:30–18:30, grace 15 |
| Work Location | PASS | PATNA-OFFICE; coordinates NULL; radius 100 |

## APIs Created

All resources support GET collection, GET /{id}, POST, PUT /{id} and DELETE /{id}.

| Collection | Additional GET endpoint |
| --- | --- |
| /api/companies | — |
| /api/branches | /api/companies/{companyId}/branches |
| /api/departments | /api/companies/{companyId}/departments |
| /api/designations | /api/companies/{companyId}/designations |
| /api/shifts | /api/companies/{companyId}/shifts |
| /api/work-locations | /api/branches/{branchId}/work-locations |

There are 35 master operations plus GET /api/health. Swagger documents 18 paths.

## API Test Results

**PASS**. `node --test tests/day01/api-integration.test.mjs` passed all nine cases initially and after the harness fix. The final run completed in 19.63 seconds and recorded 92 HTTP operations, with additional Swagger UI, JWT and CORS checks.

For every master, the SQL-backed suite exercised create, list, read, update, deactivate/reactivate, soft-delete, duplicate-code rejection and invalid requests. Nested list endpoints were exercised where applicable. Actual outcomes: create 201; read/update 200; delete 204; invalid input 400; missing/deleted record 404; duplicate code 409; unauthenticated requests 401. Department self-parenting was rejected.

Anonymous GET /api/health returned 200 and success=true. Swagger UI returned 200 and its JSON contained the documented operations. Local frontend-origin CORS preflight passed. Real JwtBearer middleware validated an ephemeral signed token; anonymous and malformed-token calls were rejected.

The integration harness uses the actual API via WebApplicationFactory, EF SQL Server and an isolated database, with a static test issuer configuration. It does not bypass authentication. This does not test external identity-provider discovery or IIS hosting.

## Database Tables

`dbo.Companies`, `dbo.Branches`, `dbo.Departments`, `dbo.Designations`, `dbo.Shifts`, `dbo.WorkLocations`, plus `dbo.__EFMigrationsHistory`.

All six master tables share Id, creation/update timestamps and actors, and IsDeleted. Repository and SaveChanges audit/filter logic were reviewed alongside executed persistence tests.

## Frontend Screens

**PASS** for all six Settings → Organisation screens and API integration.

The browser loaded Companies, Branches, Departments, Designations, Shifts and Work Locations from the real SQL-backed API. A disposable company was created, renamed and deleted through the normal UI, including the delete confirmation. SQL independently confirmed its retained IsDeleted=1 row and creation/update actor. The disconnected error state and loading state were also observed.

Browser error logs returned an empty list. After the harness correction, the full API regression logs had zero warning/error/unhandled-exception/500 matches. The rebuilt Angular chunk hashes match the bundle used for the browser check.

Source review and passing frontend tests cover shared table/form controls, searchable tables, activation controls, DTO payload compatibility, nullable coordinates, overnight times and validation. Full browser CRUD for every master was not repeated; every master was covered by the automated API suite and frontend contract tests, with all six tables exercised in the browser.

## Tests

**138 automated test cases passed:** 118 backend, 11 frontend and 9 additional SQL-backed integration cases. Repeat executions are not counted twice. Direct SQL and browser checks are additional evidence.

Source/configuration/script/documentation scans found no hard-coded production credential patterns, private-key headers or Day 1 TODO/FIXME/HACK/NotImplementedException markers. Generated files, dependency folders and temporary artifacts were excluded. Connection/JWT configuration was reviewed separately. This is a repository check, not a scan of external servers or secret stores.

Seed source and SQL assertions confirmed PAN, TAN, GSTIN, PF and ESI identifiers remain NULL; no fake registration information was inserted. Patna coordinates remain NULL. General Shift duration thresholds remain unconfigured.

Reproduction instructions: [Day 1 tests](../tests/day01/README.md). HTTP evidence: `artifacts/day01/http-evidence.json`. Backend TRX evidence: each test project's `TestResults/day01-final.trx`. Final API logs: `artifacts/day01/final-retest-host.log` and `final-retest-host-error.log`. Generated evidence is ignored by version control.

## Bugs Found

- Test harness copied the API response content into Kestrel even for HTTP 204. Kestrel logged an invalid body-write exception on OPTIONS/DELETE, although the HTTP status and business operation succeeded.
- Initial clean frontend installation encountered a transient Windows EBUSY directory lock.

No Day 1 product-code defect was identified by the executed checks.

## Bugs Fixed

- Updated `tests/LCAP.HRMS.Day01Harness/HarnessProgram.cs` to omit body copying for bodyless status codes. Rebuilt the harness and solution, reran the frontend build, and reran all nine SQL-backed integration cases and direct SQL checks on another fresh database. Final server logs are clean.
- Retried the clean dependency installation successfully, then passed frontend tests and builds. No dependency version changes were necessary.

## Known Issues

- Actual external OIDC login, refresh, discovery/key rotation, IIS deployment and production HTTPS/SQL certificates were not exercised by the local harness.
- The frontend currently uses a memory-only token-entry form and a placeholder Administrator profile.
- Company seed payroll/payment days are 1/1; General Shift half/full-day thresholds are NULL. Confirm operational values before future processing.
- Geofence coordinates must be supplied from the actual office location before using geofencing for attendance.

## Technical Debt

- Authentication exists; role-based and company-scoped authorization remain pending before sensitive employee data is exposed.
- Master search/paging is performed client-side over fetched data. Future larger datasets need appropriate server-side interaction.
- Full-record PUT has no optimistic concurrency token.
- Work Location company/branch consistency is checked by the application service; the SQL schema uses separate foreign keys rather than a composite ownership constraint.
- Health checks liveness, not database readiness. Production performance, backup/recovery and future upgrade scenarios remain outside this audit.

## Day 2 Readiness

**YES** for starting Employee Master development on the validated Day 1 foundation. This is not approval for production rollout.

## Pending for Day 2

Employee Master, Reporting Manager relationship, and Employee links to Company, Branch, Department, Designation, Shift and Work Location. Confirm required fields, ownership rules, relationship optionality, code uniqueness and reporting-cycle validation before implementation. See [Day 2 handoff](DAY-02-HANDOFF.md).

## Build Result

| Area | Final result |
| --- | --- |
| Backend | PASS |
| Frontend | PASS |
| Database | PASS |
| API | PASS |
| Day 1 Masters | PASS |
| Ready for Day 2 | YES |

No unresolved failures remain in the executed Day 1 checks.
