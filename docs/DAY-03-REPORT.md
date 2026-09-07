# LCAP HRMS - Day 3 Report

## Final Status

**PASS** — verified on 7 September 2026. Backend, frontend, SQL migrations, geo-distance validation, check-in/out, authorization and browser integration passed the executed checks. Day 3 implements geo-fenced attendance only; no Day 4 policy or penalty logic is included. Physical Patna GPS validation remains pending actual office coordinates.

## Completed

- Attendance entity, DTOs, application service, repository, controller, dependency injection and SQL migration.
- Self-service employee check-in/check-out and scoped, read-only HR register.
- Server-side distance, GPS quality/age validation, timezone resolution and duplicate protection.
- Angular attendance route with today card, history, HR filters, loading/error/success feedback and click-triggered geolocation.
- Existing Day 1/2 regression suites retained.

## Attendance Architecture

`API → AttendanceService → AttendanceRepository / existing IUnitOfWork → ApplicationDbContext`.

The Domain owns AttendanceRecord and the two Day 3 statuses: CheckedIn (1) and Completed (2). The Application owns identity/eligibility validation, the date resolver and Haversine calculation. Infrastructure owns EF mapping and transaction locking. The existing unit of work and audit conventions are reused; there is no additional generic abstraction or attendance policy engine.

## Database/Migrations

Migration: `20260907162713_GeoAttendance`.

- `dbo.AttendanceRecords` has restrictive Company, Employee, WorkLocation and Shift foreign keys; SQL Server uses NO_ACTION. No cascade deletes.
- GPS columns use decimal(10,7), accuracy decimal(12,3), distance decimal(14,3), attendance date `date`, timestamps `datetimeoffset` stored with UTC offsets.
- Unfiltered unique EmployeeId + AttendanceDate reserves even soft-deleted dates. A filtered unique EmployeeId index permits one nondeleted open record across dates.
- CompanyId + AttendanceDate and WorkLocationId + AttendanceDate indexes support searches.
- Check constraints validate coordinate bounds, positive accuracy, nonnegative distance, timestamps and status consistency.
- Fresh isolated database: `LCAP_HRMS_Day03_20260907_Final`, SQL Server 2022 16.0.1000.6, compatibility level 160. All nine migrations applied; sequential idempotent reapplication passed.
- Direct SQL tests verified keys, precision, audit values, UTC offsets, ownership, unique/check constraints and deletion restrictions.
- Permanent PATNA-OFFICE coordinates remain NULL; LCAP legal registration seeds remain NULL. Only isolated TEST-* work locations have synthetic coordinates.
- EF model comparison reports no pending changes. No production database was updated.

## Geo Distance Engine

`IGeoDistanceService / GeoDistanceService` uses Haversine with mean Earth radius 6,371,008.8 metres. Inputs are checked for valid, finite coordinates; the intermediate value is clamped for numerical stability. Backend distance is authoritative. The full-precision result is compared with the radius before storage rounding: equality is inside.

Known-distance tests cover identical points, one equatorial degree, antimeridian crossing and opposite poles. A deterministic test double verifies exact-boundary acceptance and just-outside rejection.

## GPS Validation

Latitude/longitude and positive representable accuracy are required for both actions. Browser coordinates are requested with `enableHighAccuracy: true`, a 15-second timeout and `maximumAge: 0`. There is no continuous/background location tracking.

Configuration in API appsettings:

- `Attendance:MaximumGpsAccuracyMeters`: 100.
- `Attendance:MaximumLocationAgeSeconds`: 120.
- `Attendance:MaximumFutureClockSkewSeconds`: 30.
- `Attendance:DefaultTimeZone`: UTC.
- `Attendance:CompanyTimeZones:ec8af472-df65-43d4-9abf-9378e553e455`: Asia/Kolkata.
- `Attendance:BranchTimeZones`: optional branch overrides using canonical GUID strings as keys.

A supplied browser timestamp is checked for age and future skew. It is never the official attendance timestamp. The API allows omitted diagnostic timestamps; the Angular client always sends the Geolocation API timestamp.

Enabled geofences require a valid office coordinate pair and positive radius. Missing configuration returns a safe 409 and a configuration warning containing only the work-location ID. Disabled geofences still require valid GPS/accuracy/age, but distance and WithinGeofence are NULL and the UI says “Not enforced”.

## Check-In

Employee/company identity comes from validated JWT claims. Employee must exist, not be deleted, be active with Active/OnNotice employment status, and have valid active organisation, shift and work-location assignments. Company/branch/location ownership must agree. The service validates GPS, calculates distance, resolves the attendance date and saves server UTC time plus GPS evidence. Duplicate dates/open records return 409. Accepted/rejected service attempts log identifiers and a controlled category, never precise location.

## Check-Out

Checkout finds the employee's open record and enforces current eligibility, unchanged shift/work-location assignment and GPS/geofence rules. It records server UTC checkout time and evidence, then marks Completed. No check-in or repeated checkout returns 409. Employee assignment changes are blocked while attendance is open. Organisational deletion/reparenting guards preserve referenced attendance history.

## Overnight Shift Handling

Branch timezone overrides company timezone, which overrides the default. For a shift with EndTime earlier than StartTime, a check-in before the local end time belongs to the preceding date. Checkout always closes the original open record, including after midnight. Timezone ID and AttendanceDate are saved on the record. Tests cover a 22:00–06:00 shift and timezone/date boundaries. No lateness, half-day or duration evaluation is performed.

## Authorization

- Every attendance endpoint requires authentication.
- Self actions require both signed `employee_id` and matching `company_id`, including for privileged roles.
- Check-in/out DTOs reject unexpected fields, including caller-supplied employee/company IDs.
- Employees and managers read their own attendance; no manual write/edit/delete endpoints exist.
- HRAdmin reads only its company; SuperAdmin can read across companies.
- DTOs omit exact GPS, government identifiers, bank information and contact details. Responses are marked no-store.
- Integration tests use real signed JWTs in an isolated loopback-only test host. The test issuer and GPS shim are absent from the production API/frontend.

## APIs

| Method | Route | Result |
|---|---|---|
| POST | `/api/attendance/check-in` | 201, or safe 400/401/403/409 |
| POST | `/api/attendance/check-out` | 200, or safe 400/401/403/409 |
| GET | `/api/attendance/me/today` | Resolved day/timezone and optional record |
| GET | `/api/attendance/me` | Paged own history |
| GET | `/api/attendance/{id}` | Own or authorized HR record; 404 when absent |
| GET | `/api/attendance` | Scoped HR register |

Search supports date/fromDate/toDate/status, employee and branch filters where applicable, plus skip/take (maximum 1,000). All six endpoints appear in Swagger with documented response types. Health and Swagger were fetched successfully from the test host.

## Frontend

`/attendance` is connected to the existing in-memory bearer-token session and relative `/api` routes. Employee today card transitions Not Checked In → Checked In → Completed. Buttons disable during location acquisition, save and reload. Permission denial, unavailable location, timeout and safe API messages are displayed without raw exceptions. History is paged at 50 rows. HRAdmin/SuperAdmin receive a read-only register with date, branch and employee selectors; exact coordinates are never displayed.

Executed browser flow against real SQL-backed API: synthetic employee check-in, successful checkout on the same record, Asia/Kolkata display, HR register and date/branch/employee filtering. Browser console captured no warnings/errors. Evidence is under ignored `artifacts/day03/`.

## Tests

Executed checks:

- 175 backend tests pass: 135 API integration tests and 40 infrastructure/geo tests, including existing Day 1/2 regressions.
- 31 frontend tests pass, including geolocation permission/unavailable/timeout, pending location, fresh request options, state transitions, API rejection propagation and loading cleanup.
- Nine real SQL-backed API scenarios pass, including four simultaneous check-ins (201/409/409/409) and four simultaneous checkouts (200/409/409/409).
- Direct SQL verification passed for migration history, foreign keys, indexes, evidence, seed integrity and constraints.
- Browser employee and HR flows passed; check-in/check-out evidence and read-only filters were visually verified.

Backend attendance coverage includes inside/outside/boundary, invalid/missing GPS, accuracy, stale/future timestamps, inactive/deleted employee, unavailable assignments/location, missing office coordinates, disabled fence behavior, duplicate actions, no check-in, forged identity, company isolation, exact distance, reserved deleted attendance dates, overnight shift, missing employee claim and configuration binding. Concurrent requests are tested against SQL Server rather than inferred from SQLite.

Reproducible instructions: `tests/day03/README.md`. Raw build/test/SQL logs and browser captures: `artifacts/day03/` (intentionally untracked; ephemeral tokens are removed after the audit).

## Security Checks

Source/config secret-pattern scan found zero private-key, common API-key or inline database password matches. Production SQL uses an integrated-security placeholder; identity-provider configuration remains external. Permanent statutory registration and Patna-coordinate seeds were checked directly in SQL. Attendance source has no unfinished TODO/FIXME/NotImplementedException or continuous GPS watcher. API logs contain expected controlled configuration warnings during negative tests and no unhandled application errors.

## Bugs Found

1. Configuration binder silently skipped GUID-keyed timezone dictionary entries, causing LCAP to fall back to UTC.
2. Initial infrastructure build lacked the configuration binding package.
3. A negative test fixture attempted to soft-delete an assigned master through the guarded normal path before reaching attendance validation.
4. The existing sidebar always highlighted Settings while the new Attendance route was open.
5. SQLCMD's default quoted-identifier setting prevented filtered-index creation in an initial test script invocation.

## Bugs Fixed

1. Timezone keys now bind as case-insensitive strings; a configuration-binding regression test and API timezone assertions verify Asia/Kolkata.
2. Added the .NET 8 configuration binder dependency and logging abstractions used by the application service.
3. Legacy unavailable-assignment fixtures use isolated direct SQL; production assignment guards remain enforced.
4. Sidebar highlighting follows the active route.
5. SQL script instructions use `sqlcmd -I` and sequential application. A fresh final database was migrated successfully and reapplied sequentially.

## Known Issues

- Real-world Patna geofence validation awaits actual office coordinates entered through the existing Work Location screen. Synthetic browser testing is not physical GPS testing.
- Browser GPS/accuracy/timestamps are client assertions; the backend checks them but cannot prove physical presence or defeat GPS spoofing.
- Omitted client timestamps cannot be checked for staleness. No raw diagnostic timestamp is retained.
- No stale-open-record cutoff or attendance correction workflow exists in Day 3. A record stays open until a valid checkout; deactivation/configuration changes may require HR to restore valid eligibility/configuration before checkout.
- Existing identity-provider sign-in is still a deployment prerequisite; the frontend uses the pre-existing token connection dialog.

## Technical Debt

- Define GPS evidence retention/access controls and stronger device attestation requirements before production rollout.
- History preserves assignment IDs/day/timezone/evidence, but names come from current master data rather than immutable name snapshots. DeviceIdentifier/UserAgent are check-in diagnostics; no separate checkout device fingerprint is retained.
- HR filter dropdowns currently load at most 1,000 options; a server-searched selector is a future scale improvement.
- Existing Day 1 master authorization remains a separate hardening item; Day 3 attendance enforces its own role/company scope.
- Serializable transactions and per-employee SQL application locks protect attendance writes. High-load retry/backpressure behavior should be profiled before deployment.

## Day 4 Readiness

**Ready for Day 4: YES.** Implementation, final regression tests and audit evidence are complete. See `DAY-04-HANDOFF.md`. Do not add policy fields or penalties to raw evidence handling without an explicit design for reevaluation and idempotency.

## Build Result

| Check | Executed result |
|---|---|
| Backend restore | PASS |
| Backend Release solution build | PASS — 0 warnings, 0 errors |
| Backend tests | PASS — 175 passed, 0 failed, 0 skipped |
| EF model comparison | PASS — no pending changes |
| Fresh SQL migrations + sequential reapply | PASS — nine migrations |
| SQL schema/evidence/constraint verification | PASS |
| Real signed-JWT SQL API scenarios | PASS — 9 passed |
| Frontend dependency tree | PASS |
| Frontend tests | PASS — 31 passed, 0 failed |
| Angular production build | PASS |
| Browser employee check-in/out and HR filters | PASS |
| Browser console | PASS — no captured warnings/errors |

Build and audit artifacts are in `artifacts/day03/`. The isolated test host was stopped after verification; temporary JWT files were removed. No production database, permanent seed coordinates, committed Day 1/2 migrations, or later-day business rules were changed.

