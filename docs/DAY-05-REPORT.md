# LCAP HRMS - Day 5 Report

## Final Status

**Final Status: PASS. Ready for Day 6: YES.** Audit date: 8 September 2026 (Asia/Kolkata). Results below describe executed local checks; they are not a production deployment certification.

## Completed

Attendance regularisation submission, employee history/cancellation, fixed reporting-manager review, HR visibility, immutable corrections, effective attendance reads, controlled evaluation revisions, explicit penalty-impact review flags, audit history and Angular screens.

No payroll, salary deductions, leave, attendance locking, payslips, direct manual attendance editing, payment processing or Day 6 features were implemented. Notifications use the request/status screens; no email/SMS or notification subsystem was added.

## Regularisation Architecture

Raw AttendanceRecord → AttendanceRegularisationRequest → stored manager review → AttendanceCorrection → effective attendance → appended AttendanceEvaluationRevision.

Application services depend on a narrow persistence contract; Infrastructure implements it using the existing DbContext and per-employee transaction/SQL lock. Corrections, revisions and audit entries are append-only. Existing Day 4 evaluations and penalty events keep their immutability guards. Normal regularisation endpoints never update raw timestamps or GPS fields.

Requests store raw timestamps, previous effective timestamps/correction ID, attendance date, timezone, shift information, branch and the assigned manager. This also detects evidence/correction changes between submission and approval: approval returns 409 and asks for cancellation/resubmission instead of applying stale assumptions.

## Request Types

| Type | Proposed values and validation |
|---|---|
| MissedCheckIn | Check-in only; existing effective check-in must be absent |
| MissedCheckOut | Check-out only; effective check-in must exist and checkout must be absent |
| IncorrectCheckIn | Check-in only; original effective check-in must exist |
| IncorrectCheckOut | Check-out only; original effective checkout must exist |
| IncorrectBoth | Both values; both effective original punches must exist |
| AttendanceNotRecorded | Both values; neither raw attendance nor a previous correction may exist |
| Other | Required reason, optional proposed values |

Date and trimmed reason are required. Reason, supporting note and review/cancellation remarks are bounded to 2,000 characters. Attachment URL, when supplied through the API, must be HTTPS; no upload/download integration was added. Normal request bodies reject unknown fields, including caller-supplied company/employee identity.

No future attendance date or proposed instant is accepted. Proposed instants use explicit offsets and are interpreted in the saved attendance timezone. Check-in must resolve to the requested shift date. Checkout must follow effective check-in, with at most a 36-hour span; next-calendar-day checkout is supported. A reason-only exception remains unevaluated and does not erase a carried late sequence. Monthly reset semantics still apply.

## Workflow / State Machine

Submission goes directly to PendingManager. Valid transitions are PendingManager → Approved/Rejected/Cancelled, then Approved → Applied. Approval and application happen in one transaction: clients see Applied on successful approval; both Approved and Applied-related actions remain in the audit trail. There is no public apply or direct-edit endpoint.

Rejection requires remarks. Cancellation requires a reason and is allowed only for the owning employee while pending. Reviewed/cancelled requests cannot be reviewed again; invalid transitions return 409. History cannot be soft-deleted or changed through the DbContext workflow.

Overlapping active requests on the same employee/date are rejected. Check-in variants overlap each other, checkout variants overlap each other, and Both/NotRecorded/Other overlap either side. Submitted/PendingManager/Approved are active. A filtered database unique index additionally protects employee/date/type. Rejected, Cancelled and Applied requests remain available for history and later submissions.

## Reporting Manager Approval

Submission requires an active, non-deleted same-company reporting-manager employee, distinct from the requester. Review additionally requires a Manager-role JWT whose employee ID equals the stored reviewer, and an eligible same-company manager account. HR/SuperAdmin have no approval override.

A request assigned to Manager A stays with A after the employee changes to Manager B; new requests use B. No automatic reassignment/delegation is provided. Manager role provisioning remains an identity-provider responsibility.

## Attendance Correction

Every approved request creates one unique correction containing full effective check-in/out values, its original raw reference (nullable), request ID, version, reason and application audit fields. Unchanged punch values come from the previous effective snapshot. Later corrections are selected by highest version; earlier corrections remain immutable.

Missed checkout leaves raw CheckOutTime null and stores the effective checkout separately. The previous unique raw-open index was replaced by a nonunique filtered lookup index, because an immutable raw null can represent an effectively closed day. The existing serialized raw check-in operation now excludes corrected-closed records. A correction-only date also reserves that date against subsequent raw check-in. Both cases have regression tests.

## Effective Attendance

`IAttendanceEffectiveStateService` returns original and effective times, Raw/Regularised source, raw/request/correction identifiers, latest derived evaluation and any review flag. Correction-only attendance has a null raw record ID and no fabricated GPS.

Existing raw attendance responses now include a separate Effective object, using batched correction/revision reads. Original fields and Day 4 immutable evaluation reads retain their original meaning. The attendance UI labels raw evaluation separately from effective attendance. Existing raw-history filters continue to filter raw evidence; they are not a corrected payroll register. Correction-only historical days are available through regularisation history and the effective-date API without inventing raw rows.

## Re-Evaluation

Approval appends a new evaluation revision for the corrected date and subsequent recorded/corrected dates, recomputing chronological sequence effects under the original policy snapshots. Missing raw evaluations use the effective company policy when first evaluated; once a revision exists its snapshot is retained. Original no-policy outcomes are preserved safely.

Each revision has employee/date/version uniqueness, a triggering correction, original evaluation reference where available, and a serialized typed result containing the policy/shift/timezone snapshot and calculated values. Old evaluations/revisions are not overwritten. Future raw check-ins and the current late summary use the latest corrected prior sequence.

The original Day 4 late/grace/reset rules remain in use. An approved 09:40 correction for raw 09:50 becomes Present when the grace boundary is 09:45. The original Late evaluation remains readable. Reason-only exceptions do not count as attendance, and do not reset the sequence simply because no punch exists.

## Penalty Event Impact

Day 4 Pending events are never deleted, cancelled or consumed by Day 5. Recalculation conservatively records PolicyImpactReviewRequired when a historical event exists, when a recomputed rule qualifies for an event, or when an earlier revision already required review. The audit trail records this explicitly, and penalty read DTOs and HR screens expose it.

No replacement penalty event is generated during correction recalculation. A qualifying result in a revision indicates rule impact requiring review, not a new deduction or an automatically approved replacement event. A future reconciliation/payroll consumer must inspect these flags and resolve them before consuming affected events. A dedicated resolution workflow remains deferred as permitted by the Day 5 scope.

## Authorization

- Employees: submit/read own requests, read own effective attendance, cancel own pending requests.
- Managers: only stored-assignment inbox/detail/audit and approve/reject operations; no access to another manager's queue.
- HRAdmin: read-only company register/detail/effective views.
- SuperAdmin: cross-company read where existing access policy allows; no review override.
- PayrollAdmin: no special regularisation write/review permission; only normal own-employee operations with valid identity claims.

JWT identity is authoritative. API tests include anonymous 401, forbidden 403, foreign-company scope and identity spoof rejection. UI role visibility supplements server enforcement.

## APIs

| Method | Route |
|---|---|
| POST | /api/attendance-regularisations |
| GET | /api/attendance-regularisations/me |
| GET | /api/attendance-regularisations/me/{id} |
| POST | /api/attendance-regularisations/{id}/cancel |
| GET | /api/attendance-regularisations/my-team |
| POST | /api/attendance-regularisations/{id}/approve |
| POST | /api/attendance-regularisations/{id}/reject |
| GET | /api/attendance-regularisations |
| GET | /api/attendance-regularisations/{id} |
| GET | /api/attendance-regularisations/{id}/audit |
| GET | /api/attendance/effective/me?date=YYYY-MM-DD |
| GET | /api/attendance/effective?employeeId=...&date=YYYY-MM-DD |

Lists have bounded pagination (maximum 200) and status/employee/manager/branch/date/date-range filters as appropriate to scope. Swagger documents the routes and response codes. Existing health/Swagger endpoints remain operational.

## Frontend

- `/attendance/regularisation/new`: dynamic fields, original/effective preview, attendance-timezone conversion, reason/note, loading/error/submission states and assigned-manager confirmation.
- `/attendance/regularisations`: own status history, details/audit and pending-only cancellation.
- `/manager/attendance-approvals`: assigned inbox, filters, view, approve with optional remarks and reject with mandatory remarks.
- `/attendance/regularisations/admin`: read-only HR register with status, branch, manager, employee and date-range filters.
- `/attendance`: request/history/inbox/register navigation and separate effective attendance display.
- Existing penalty register shows Policy impact review required.

Browser verification passed required-field validation, 09:40 submission with original 09:50 preview, manager approval, mandatory rejection validation, rejection with remarks, employee Applied/Rejected/Cancelled history, employee denial on the manager screen, and HR employee/status filtering with read-only actions. Review-dialog validation was corrected and visually retested. No captured browser console warnings/errors.

## Database / Migration

Migration: `20260907195305_AttendanceRegularisation` (eleventh migration).

Tables: AttendanceRegularisationRequests, AttendanceCorrections, AttendanceEvaluationRevisions, RegularisationAuditEntries. New tables have 22 restrictive NO_ACTION foreign keys. Indexes include company/status, employee/date, manager/status, raw references, active request uniqueness, unique correction per request and unique revision per employee/date/version.

All migrations were applied to fresh isolated `LCAP_HRMS_Day05_20260908_Audit` on SQL Server 2022 **16.0.1000.6**, compatibility **160**, then replayed sequentially using an idempotent script and `sqlcmd -I -b`. SQL constraint checks passed. Existing migrations were not rewritten. No business seed, production database, permanent office coordinates or legal registration information was changed.

## Tests

Executed regression suite: **247 backend tests passed** (63 infrastructure/domain + 184 API), including **35 new regularisation cases**. No failures or skips. Frontend: **72 passed**, including 23 new regularisation checks. Seven signed-JWT SQL scenario groups passed.

Coverage includes all seven request types; time/date/reason/type validation; missing/inactive/deleted/foreign managers; overlapping requests; self/team/company isolation; approve/reject/cancel state restrictions; immutable correction/request history; raw GPS/timestamp preservation; original and revised evaluations; missing-raw and overnight corrections; manager changes; stale snapshot conflict; next check-in after corrected checkout; correction-only date reservation; forward late-sequence recomputation; and reason-only exception sequence preservation.

SQL scenarios separately exercised realistic penalty-impact correction, simultaneous duplicate submission, approve/reject race, two approvals, no-raw attendance, corrected missed checkout followed by normal check-in, HR isolation and historical reviewer assignment. Existing Day 1–4 organisation, employee, reporting hierarchy, attendance, geofence, policy, late-rule and concurrency tests were included in the full regression run.

Frontend checks cover dynamic fields, required reason/times, overnight/Asia-Kolkata conversion, DST invalid/ambiguous input, permissions, status labels, cancellation gating and UI contract/state checks. These complement the real browser flows rather than substituting for them.

## Concurrency

The existing per-employee SQL application lock and serializable transaction serialize submissions/reviews/cancellation/raw punches. Review reloads the request after acquiring the lock. Status is also an optimistic concurrency token, and conflicts map to 409. SQL uniqueness protects active duplicate requests, correction application and revision versions.

Executed races: duplicate submission → one 201/one 409; approve versus reject → one 200/one 409; two approvals → one 200/one 409 and one correction. Repeated review returns 409. Approval/application/re-evaluation/audit commit atomically, so a failed application does not leave an approved-but-unapplied partial workflow.

## Audit Trail

Append-only entries record actor, server timestamp, request/employee/company, action and reason. Actions include Submitted, Cancelled, Approved, Rejected, CorrectionApplied, EffectiveStateChanged, ReevaluationTriggered and PolicyImpactReviewRequired. Request/correction/revision base audit fields are populated normally. Audit reads are authorized using the request scope. Logs use identifiers/actions and do not log precise GPS.

## Security Checks

Source scan/review found no hard-coded production credentials or unfinished Day 5 TODO/FIXME markers. New normal DTOs omit GPS/bank/payroll fields. Attachment URLs are validated but never fetched by the backend. Templates render text with Angular interpolation. Real JWT tests exercised role/company/self boundaries.

Test keys are generated per harness process, test tokens remain ignored local artifacts and are removed after audit. Test clock/token/evaluation/GPS helpers exist only in the separate loopback test host. Synthetic GPS/data remained isolated. API logs contained no unhandled 500s in the executed scenarios; browser console inspection returned no warnings/errors.

## Bugs Found

1. The old raw-open unique index could block a new check-in after a corrected missed checkout.
2. Correction-only days needed explicit date reservation against duplicate raw check-in.
3. Rejection validation was displayed behind the review dialog.
4. A reason-only exception could incorrectly clear a prior late sequence.
5. Initial development checks exposed a controller helper-name collision/rename typo, a SQLite decimal-fixture comparison limitation and missing mandatory payroll-day values in a synthetic company fixture.

## Bugs Fixed

1. Replaced the raw-open uniqueness index with a lookup index and retained serialized effective-open checks; regression and real SQL scenario passed.
2. Date existence checks now include corrections; added API regression.
3. Moved validation into the dialog, improved review spacing and retested visibly in the browser.
4. Unevaluated reason-only exceptions preserve the compatible carried sequence, respecting monthly boundaries; added regression.
5. Corrected the helper name and isolated test fixtures. Product remains .NET 8; final build checks are recorded below.

## Known Issues

- Penalty impact is explicitly flagged for later resolution; there is no cancellation/replacement-event approval or payroll consumer in Day 5.
- Request statuses provide notification visibility; no push/email/SMS notifications, manager reassignment or delegation workflow exists.
- Identity-provider configuration and correct Manager claims, actual Patna coordinates and physical-device GPS verification remain inherited production prerequisites.
- Correction-only attendance is not inserted into the raw attendance table/history. Use effective reads and regularisation history for those dates; further changes use another reviewed request.

## Technical Debt

- Re-evaluation loads chronological history and appends affected revisions synchronously. Histories over 5,000 days fail safely with 409 and transaction rollback; a supervised background/batch process is future work. Load-test longer histories and concurrent workloads before production.
- JSON evaluation snapshots need schema compatibility discipline when their typed contract changes.
- Penalty impact reconciliation must become an explicit audited workflow before payroll consumption. Review flags are conservative and persistent.
- Existing raw-history late/penalty filters intentionally retain raw-evidence semantics; a future corrected reporting/payroll query should use effective states/revisions.
- HR selectors load at most 1,000 choices; manager choices derive from loaded assigned requests. Search/paging improvements remain a scale concern.
- Historical manager IDs are fixed; display names and some assignment labels use current master data. Old shift-end validation cannot reconstruct a historical end time that Day 3 did not snapshot.
- Downgrading this migration after corrected checkouts may conflict with the old raw-open uniqueness invariant. Review data before any rollback; no automatic production rollback or database update is provided.
- Inherited Day 1 master authorization hardening, GPS retention/device-attestation and deployment setup remain as recorded in earlier reports.

## Day 6 Readiness

Implementation and regression evidence are complete; see `DAY-06-HANDOFF.md`. Leave functionality has not been implemented. Preserve raw evidence, revision history and penalty-impact safeguards when adding leave interactions.

## Build Result

| Check | Executed result |
|---|---|
| Backend restore | PASS |
| Backend Release solution build | PASS — 0 warnings, 0 errors; harness also passed |
| Backend tests | PASS — 247 passed, 0 failed, 0 skipped |
| Frontend dependency tree | PASS |
| Frontend tests | PASS — 72 passed |
| Angular production build | PASS |
| EF pending-model check | PASS — no changes |
| Fresh SQL migration + sequential replay | PASS — eleven migrations |
| SQL constraints/evidence checks | PASS |
| Real signed-JWT SQL scenarios | PASS — seven groups |
| Browser employee/manager/HR checks | PASS |
| Browser console | PASS — no captured warnings/errors |
| Final rebuilt SQL/API smoke | PASS — health, Swagger, correction persistence, reason-only sequence and next late trigger |

Safe logs/screenshots are retained in ignored `artifacts/day05/`. Reproduction instructions are in `tests/day05/README.md`. The isolated database is retained for inspection; test host/tokens are cleaned up after verification. No production deployment, commit or tag is implied.

