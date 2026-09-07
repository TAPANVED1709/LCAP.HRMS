# LCAP HRMS - Day 4 Handoff

## Starting point

Day 3 adds authenticated geo-fenced self attendance, UTC check-in/out evidence, a resolved attendance date and timezone snapshot, a SQL migration, employee UI and a read-only HR register. See DAY-03-REPORT.md for executed evidence and limitations.

Raw attendance has two states only: CheckedIn (1) and Completed (2). It does not mean Present/Late/Absent/HalfDay/LOP. Preserve raw timestamps, location evidence, audit fields and employee/company scope.

## Day 4 target

- Attendance Policy Master.
- Shift attendance evaluation.
- Grace-period application and late detection.
- Consecutive late counter.
- LCAP three-late rule.
- Penalty event generation.

None of these rules is implemented during Day 3. Leave, payroll, salary deductions, regularisation and attendance editing remain outside this delivery.

## Decisions needed before implementation

Clarify the LCAP three-late rule: what counts as late; applicable grace; whether consecutive means calendar days or scheduled working days; reset conditions; month boundaries; holidays/approved leave; and what event is generated after the third late occurrence. Do not invent policy values or monetary deductions. Define effective dates and precedence of company/branch/employee policy assignment.

Decide how incomplete/open attendance, assignment changes, overnight shifts and reevaluation after policy changes should be handled. No automated stale-open closure exists today.

## Integration points

- `AttendanceRecord`: CompanyId, EmployeeId, WorkLocationId, ShiftId, AttendanceDate, TimeZoneId, UTC timestamps and immutable source evidence.
- `AttendanceDateResolver`: company/branch timezone and overnight start-date rule. Checkout remains on its original record.
- Existing Shift master: StartTime, EndTime, GracePeriodMinutes, minimum half/full-day minutes. These fields are not evaluated by Day 3.
- Existing employee assignments and JWT scope are authoritative. Preserve self-only attendance writes and HR read scope.
- Existing IUnitOfWork/audit/soft-delete conventions should be reused.

Prefer a separate derived evaluation/event model so policy changes do not overwrite raw attendance evidence. Design a unique evaluation/event identity and safe rerun behavior before introducing scheduled processing. Penalty events are not payroll deductions.

## Database and deployment

Latest migration is `20260907162713_GeoAttendance`; there are nine migrations. Do not rewrite committed Day 1/2 migrations. Generate new reviewed migrations for Day 4 and apply only to an explicitly selected development/test database. SQLCMD scripts require `-I` for quoted identifiers/filtered indexes.

LCAP timezone is configured as Asia/Kolkata using its canonical company GUID string; optional branch overrides take precedence. Do not replace PATNA-OFFICE NULL coordinates with guesses. Use isolated TEST-* locations for automation.

Production requires an actual identity provider, HTTPS on IIS for browser geolocation, valid database configuration/certificate, real office coordinates and a location-data retention policy. Test harness issuers and GPS shims must never be deployed.

## Required regression checks

Retain Day 1/2 tests and Day 3 geofence, role/company scope, timestamp, unique/concurrent action, soft-delete reservation and overnight tests. Add policy boundary tests (exact grace, just beyond grace), consecutive/reset/holiday behavior, overnight evaluation, effective dates and idempotent event generation. Test policy reevaluation without changing raw GPS evidence.

Build both applications, compare the EF model, migrate a fresh isolated SQL Server database, test real signed-JWT API calls and verify employee/HR screens. Update the daily report with executed results; never infer PASS from compilation alone.
