# LCAP HRMS - Day 6 Handoff

Day 5 implements reviewed regularisation with immutable corrections, effective attendance and evaluation revisions. Read `DAY-05-REPORT.md` for executed checks, semantics and limitations. Day 6 is not implemented.

## Day 6 Scope

- Leave Master and leave types.
- Employee leave requests.
- Reporting Manager approval/rejection.
- Leave-balance foundation.
- Attendance and leave interaction.
- Basic leave calendar.

## Boundaries to Preserve

Raw AttendanceRecord timestamps and GPS remain evidence. Approved corrections live separately in AttendanceCorrections; latest active version supplies effective times. AttendanceEvaluationRevision preserves recomputed outcomes and original evaluation references. Day 4 penalty events remain immutable Pending records; PolicyImpactReviewRequired flags require explicit reconciliation before any future consumption.

Do not implement leave by deleting attendance, fabricating check-ins, overwriting corrections, silently changing evaluations or deducting salary. Design attendance/leave conflicts and any approved reevaluation as an explicit audited workflow.

## Existing Integration Points

- `IRegularisationService`: JWT-derived self submission, fixed manager review assignment, scoped histories and immutable audit trail.
- `IAttendanceEffectiveStateService`: original/effective times, source, correction/request IDs and latest evaluation/review flag.
- `CorrectionEvaluationService`: chronological recomputation using preserved policy snapshots; new revisions, no old-event deletion.
- `AttendanceDateResolver`: overnight date and branch/company/default timezone rules.
- `Employee.ReportingManagerId`: resolve on submission and retain the reviewer on the request. Current hierarchy changes do not redirect existing requests.
- Existing per-employee SQL lock: reuse a consistent lock order when attendance and leave decisions overlap.

## Decisions Before Implementation

Define leave units, half-day boundaries, accrual/opening balance rules, negative-balance policy, date inclusivity, holidays/weekends and overlapping leave. Establish who can submit/review/cancel, manager eligibility and self-approval prevention. Specify exactly how approved leave interacts with raw attendance, corrected attendance, incomplete punches and late sequences.

Model balance movements as auditable transactions with idempotency and concurrency protection. Specify review/cancellation and reapplication behavior before mutating balances. Plan a clear future resolution path for existing PolicyImpactReviewRequired outcomes; Day 5 does not resolve or consume them.

## Regression Baseline

247 backend tests and 72 frontend tests passed; seven real SQL scenario groups plus employee/manager/HR browser checks passed. The eleventh migration is `20260907195305_AttendanceRegularisation`. Day 5 adds four tables and changes the raw-open index to accommodate immutable corrected checkouts. Original migrations remain unchanged.

Keep tests for raw GPS/timestamp preservation, manager snapshot assignment, company/self/team isolation, single-winner review races, duplicate application prevention, overnight corrections, effective-state reads, reason-only sequence preservation, forward re-evaluation and pending-event retention. Add leave-specific balance/overlap/concurrency tests, then run all prior suites and fresh SQL migration replay.

## Deferred Production Work

Identity-provider/role provisioning, actual office coordinates, physical GPS verification, audit retention, Day 1 master authorization hardening, selector scale and load testing remain deployment work. Re-evaluation is synchronous with a 5,000-day safeguard. JSON snapshot compatibility must be maintained. No payroll, payment processing, payslips or automatic salary deduction belongs in this handoff's implementation scope.
