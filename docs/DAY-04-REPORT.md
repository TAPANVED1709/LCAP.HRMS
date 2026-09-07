# LCAP HRMS - Day 4 Report

## Final Status

**PASS — Ready for Day 5: YES.** Audited on 8 September 2026 (Asia/Kolkata). This is a local implementation/integration result, not a production deployment approval.

Final execution: 212 backend tests, 49 frontend tests, seven real SQL API scenario groups, a final rebuilt-API smoke test, and browser policy/employee/HR checks passed. All test employees, coordinates and clock changes were confined to the isolated Day 4 harness/database. No production database was updated.

## Completed

Attendance policy management, company default resolution, automatic check-in evaluation, configurable late counters, immutable evaluation history, pending penalty events, scoped read APIs, HR policy/penalty screens, employee late feedback and HR attendance filters.

Existing Day 1–3 modules remain in place. No salary calculation, deduction, payroll consumption, leave, regularisation, attendance locking, manual attendance editing or approval workflow was implemented.

## Attendance Policy

- Company-scoped unique code; required company/code/name/effective date; validated grace, threshold, enums, configured values and date ranges.
- Active default periods are inclusive and cannot overlap within a company. Concurrent policy writes serialize using a company SQL application lock.
- Soft delete and activate/deactivate are supported. Codes remain reserved after soft deletion. Policies under deleted companies are hidden.
- Updates increment `Revision`. Historical snapshots retain the revision and values used at evaluation. A different policy/revision starts a new future late sequence, including after an activation or metadata edit.
- Idempotent migration seed: `LCAP-STANDARD`, grace 15, threshold 3, NextQualifyingLate, HalfDay, AfterPenalty, effective 2026-09-08, active/default. PenaltyValue is null. Its description labels it initial/configurable and subject to client approval. HalfDay is not represented as a finally approved deduction.

## Policy Assignment

Phase 1 uses the active company default effective on AttendanceDate. The resolver accepts employee ID, company ID and date so employee-specific assignment can be added later. Employee overrides are not implemented. Attendance service contains no hard-coded LCAP rule.

## Late Evaluation

Successful check-in stores raw attendance and evaluates it inside the same transaction. The boundary is the saved attendance date plus shift start in the saved attendance timezone, plus configured grace. The authoritative stored server check-in time determines lateness.

09:45:00 is not late for a 09:30 shift with 15-minute grace; 09:45:01 is late. Positive fractional late minutes round up: one second late is reported as one minute. Before-start check-ins are not late.

Raw status remains CheckedIn/Completed; evaluation independently reports Present/Late. Missing checkout does not itself cause lateness: a valid check-in can be evaluated immediately. No attendance creates no evaluation. Missing check-in or unavailable configuration is not inferred to be late.

No effective policy produces an immutable, unevaluated `PolicyNotConfigured` snapshot with nullable policy ID and no penalty. Pre-Day-4 rows without an evaluation remain visible. Repeated internal evaluation returns the existing snapshot; policy edits do not recalculate it. EvaluationVersion is 1. Shift name, timezone, policy revision and key rule values are saved.

## Consecutive Late Engine

| Configuration | Deterministic behavior |
|---|---|
| NextQualifyingLate | Threshold 3: counts 1, 2, 3; threshold reached at 3; event on late 4 |
| AfterThresholdReached | Event on the late that reaches the threshold |
| ManualReview | Calculates late status/count; no automatic event |
| AfterPenalty | Punctual days and calendar gaps preserve the sequence; an event resets the carried count to 0 |
| OnNonLateAttendance | A non-late evaluated attendance resets the count |
| Monthly | A new local attendance month resets the count; punctual days do not |
| Never | No automatic reset within the same policy revision |

Only one event is generated per uninterrupted sequence for reset modes other than AfterPenalty; counts beyond the triggering value do not generate repeated events. Missing calendar dates do not imply absence, reset or a deduction. A policy/revision change starts a new sequence. Out-of-order historical evaluation is safely flagged `HistoricalOrderReviewRequired`; it does not rewrite subsequent records.

The qualifying row retains count 4 for history while SequenceAfterEvaluation becomes 0 under AfterPenalty. The next late starts at 1. The UI separates historical count from the current summary and uses the latter for prospective warnings.

## Penalty Event

Only Pending events are generated. Each references attendance, evaluation and policy, with generated time, reason, type and optional configured value. Reserved consumption/cancellation fields remain unused. A configured value is metadata; no salary money is calculated. Read DTOs omit PenaltyValue and GPS evidence. No edit/delete/consume/cancel endpoint exists.

Evaluation and event updates/deletes are rejected by the persistence audit guard. Policy changes and generation are logged using identifiers/revision/reason; raw evidence is preserved.

## Timezone / Overnight Handling

Reuses Day 3 branch → company → configured-default timezone resolution. Overnight shifts use their start date, including the current late summary after midnight. Asia/Kolkata and overnight boundary tests pass. An ambiguous/invalid DST shift start is left unevaluated for future review instead of guessing an instant.

## Database / Migration

Migration: `20260907191311_AttendancePolicyAndLateEvaluation`.

Tables: AttendancePolicies, AttendanceEvaluations, AttendancePenaltyEvents. Existing migrations were not rewritten. EF reports no pending model changes.

Executed against fresh isolated `LCAP_HRMS_Day04_20260908_Audit` on local SQL Server **16.0.1000.6**, compatibility **160**. Applied all ten migrations and sequentially replayed the idempotent script successfully, using `sqlcmd -I -b`. Production connection settings were not replaced.

Verified ten restrictive NO_ACTION foreign keys, company/code uniqueness, one evaluation per attendance, unique event per evaluation and attendance, employee/date and company/date/status indexes, audit values, seed counts, original LCAP legal fields, null permanent Patna coordinates, and unchanged raw check-in timestamps. Direct duplicate-event insertion and referenced-policy physical deletion were rejected by SQL constraints.

## Authorization

- HRAdmin manages/reads policies and registers within its company; SuperAdmin can cross companies.
- Employee/Manager cannot manage policy or access HR registers. Self-service evaluation, summary and penalty history are limited to the claimed employee/company; team access remains deferred.
- Individual event authorization uses its saved company ownership.
- Real signed JWT tests exercised authentication and company/self isolation. Test-only token issuance lives in the separate loopback harness, not the production API.

## APIs

| Methods | Route |
|---|---|
| GET, POST | /api/attendance-policies |
| GET, PUT, DELETE | /api/attendance-policies/{id} |
| GET | /api/attendance-policies/current?companyId=...&date=YYYY-MM-DD |
| GET | /api/attendance/{attendanceId}/evaluation |
| GET | /api/attendance/evaluations |
| GET | /api/employees/{employeeId}/late-summary |
| GET | /api/attendance-penalties |
| GET | /api/attendance-penalties/{id} |
| GET | /api/employees/{employeeId}/attendance-penalties |

List APIs use bounded pagination and appropriate company/employee/branch/date/late/penalty filters. Standard envelopes and validation/403/404/409 responses are retained. Swagger contains the new endpoints and authentication metadata. Health and Swagger both returned successful responses in the SQL harness.

## Frontend

- `/attendance/policies`: searchable list, grouped validated form, create/view/edit, activate/deactivate, scoped company choices, loading/error/success feedback.
- `/attendance`: historical evaluation, current sequence, grace and late minutes, threshold warning, generated-event message; HR date-range/employee/branch/late-only/event-only filters.
- `/attendance/penalties`: read-only paginated register with date filters, reason/type/status/generated time. No salary amount or mutation actions.

Browser checks passed policy required-field validation, create, edit, view-only mode, deactivate/reactivate; employee third-late warning; fourth-late check-in and checkout; event feedback and sequence reset; HR Pending register and invalid date-filter feedback. Loading and disconnected/error feedback were observed. No captured browser console warnings/errors. Screenshots are under `artifacts/day04/browser-*.png`.

## Tests

| Executed check | Result |
|---|---|
| Infrastructure/domain suite | 63 passed |
| API integration suite, including existing Day 1–3 regressions | 149 passed |
| Frontend validation/model/source-contract tests | 49 passed |
| Real signed-JWT SQL API scenario groups | 7 passed |
| Final rebuilt API smoke and repeated evaluation | PASS |
| Browser policy, employee and HR flows | PASS |
| SQL constraints, seed and evidence verification | PASS |

Coverage includes grace boundaries, configured grace, late minutes, effective/inactive/deleted/missing policy, sequence 1–5, trigger/reset modes, chronological gaps, no attendance, missing check-in, timezone/overnight boundaries, policy revision immutability, role/company/self isolation, duplicate codes, overlapping defaults, safe DTOs, check-in/out, geofence and existing organisation/employee/reporting hierarchy behavior. Added a regression for the overnight summary at a policy end-date boundary and an assertion that a policy revision resets the current summary without changing old evaluation.

Realistic synthetic LCAP scenario: 09:48 → count1/3min, 09:49 → count2/4min, 09:46 → count3/1min/no event, 09:50 → count4/5min/one Pending event, next 09:48 → count1. Browser separately exercised another synthetic employee at threshold and the next qualifying late.

## Idempotency / Concurrency

SQL per-employee application locks plus serializable transactions and unique database indexes protect evaluation/event creation. Eight simultaneous first evaluations of the same qualifying raw record produced exactly one evaluation and event. Subsequent repeated invocations and the final rebuilt-API repeat test preserved that result. Concurrent overlapping default creation produced one 201 and one 409. Migration scripts were applied sequentially, not concurrently.

## Security Checks

Source review found no hard-coded production secrets or Day 4 unfinished TODO/FIXME markers. Harness signing keys are generated per process; temporary tokens are ignored artifacts and removed after audit. New evaluation/event API responses were checked for precise GPS and sensitive employee fields. API logs contained no unhandled failures/500s; browser console had no captured warnings/errors. No fake PAN/TAN/GST/PF/ESI information or permanent coordinates were inserted. Synthetic 0,0 coordinates exist only in isolated fixtures.

## Bugs Found

1. An initial policy-reference deletion guard broke four existing company soft-delete regression cases.
2. A historical evaluation warning could remain visible after the active policy revision changed.
3. Current summary originally used the local calendar date instead of overnight shift attendance date.
4. Individual event scope originally depended on current employee company rather than saved event ownership.

## Bugs Fixed

1. Removed the overbroad guard; policies under deleted companies are hidden while attendance/history guards and restrictive FKs remain. Full regression suite passes.
2. The current summary drives prospective warnings; historical evaluation values remain unchanged.
3. Summary reuses AttendanceDateResolver; added overnight/effective-end boundary regression.
4. Event DTO projection includes company ID and individual reads authorize against that saved ownership. Final real SQL self/HR reads passed.

## Known Issues

No outstanding Day 4 functional failures were found in the executed checks. Production prerequisites inherited from Day 3 remain: configured identity provider, actual Patna coordinates, deployment configuration and physical-device GPS testing. Synthetic browser testing does not establish physical presence or defeat GPS spoofing. Existing stale-open attendance limitations remain until a later correction workflow.

## Technical Debt

- Employee-specific policy assignment, explicit historical recalculation, approval and payroll consumption remain future work.
- Any policy update increments revision and resets future sequence; a future effective-dated revision editor could distinguish cosmetic edits.
- ManualReview and out-of-order/DST cases record safe outcomes but do not yet provide a review workflow.
- Immutable event guards must be deliberately extended with audited transitions before payroll/cancellation work.
- Current employee/branch names in projections are not full historical name snapshots; shift name is snapshotted.
- Policy/HR selectors load a bounded first 1,000 choices; server-side search is a future scale improvement.
- Profile lock timeout/retry behavior under production load. This audit tests race correctness, not load capacity.
- Existing Day 1 master authorization hardening and GPS retention/device-attestation decisions remain as recorded in the Day 3 report.

## Day 5 Readiness

**YES.** See `DAY-05-HANDOFF.md`. Immutable raw evidence, versioned evaluations and Pending events provide the starting point for explicit exception requests and review without silent rewriting.

## Build Result

| Check | Result |
|---|---|
| dotnet restore | PASS |
| Release solution build | PASS — 0 warnings, 0 errors |
| dotnet test Release | PASS — 212 passed, 0 failed, 0 skipped |
| EF pending-model check | PASS — no changes |
| Fresh migration apply + sequential replay | PASS — ten migrations |
| SQL verification | PASS |
| npm dependency tree | PASS |
| npm test | PASS — 49 passed |
| Angular production build | PASS |
| Separate Day 4 SQL harness build | PASS — 0 warnings, 0 errors |

Evidence logs are retained in ignored `artifacts/day04/`. Reproduction instructions: `tests/day04/README.md`. The isolated database is retained for inspection; the test host is stopped after verification. No commit/tag or production deployment is implied by this report.

