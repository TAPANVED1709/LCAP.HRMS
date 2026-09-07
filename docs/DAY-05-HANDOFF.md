# LCAP HRMS - Day 5 Handoff

Day 4 is implemented and locally audited. Read `DAY-04-REPORT.md` for executed results, semantics and deployment limitations. Day 5 has not been implemented.

## Target Scope

- Attendance regularisation and missed-punch correction requests.
- Reporting Manager approval/rejection, using Employee.ReportingManagerId.
- Exception workflow, an append-only audit trail, and notification/status feedback where practical.

## Preserve the Existing Boundaries

AttendanceRecord is raw evidence. AttendanceEvaluation is an immutable derived snapshot with policy revision and EvaluationVersion. AttendancePenaltyEvent is a unique Pending event, not an actual salary deduction. Day 4 check-in stores evidence and evaluation atomically under a per-employee SQL lock.

Do not overwrite raw GPS/server timestamps to implement a correction. Model the request, proposed correction, decision, reviewer, reason and timestamps separately. Define explicitly how approved corrections supersede evaluations/events; do not silently reevaluate historical attendance or delete events. Current persistence guards reject evaluation/event modifications and need an intentional, tested extension for future lifecycle transitions.

## Decisions Before Implementation

- Who may submit, review and delegate; manager vacancy/inactive manager/self-approval rules.
- Allowed request types, time windows, supporting evidence and duplicate/open-request limits.
- Corrected effective attendance representation and immutable provenance.
- Whether/how approved changes affect late sequences and already-generated Pending events. Day 4 recalculation is idempotent return-existing, not revision replacement.
- Audit retention and access, notification delivery/idempotency and rejection/resubmission behavior.
- DST/out-of-order historical review handling and concurrent request/decision protection.

## Integration Points

- CompanyDefaultAttendancePolicyResolver accepts employee/company/date for future overrides.
- AttendanceEvaluationService exposes internal EvaluateAsync plus safe scoped reads.
- Evaluation projections retain ReportingManagerId for future team authorization; manager team access is not enabled by Day 4.
- AttendanceDateResolver provides overnight/date and branch/company/default timezone behavior.
- Company/employee authorization must be enforced by the API, independently of frontend visibility.

## Regression Baseline

212 backend tests and 49 frontend tests passed. Seven real SQL API groups plus browser policy/employee/penalty checks passed. Migration `20260907191311_AttendancePolicyAndLateEvaluation` is the tenth migration. Existing Day 1–3 migrations remain unchanged.

Run all existing suites and fresh isolated SQL migration/constraint checks after Day 5 changes. Add tests for company isolation, own/team scope, self-approval prevention, duplicate/repeated decisions, concurrent approvals, immutable evidence and evaluation/event lifecycle. Maintain the fourth-late/AfterPenalty scenario until an explicit approved correction design changes it.

## Production Prerequisites and Deferred Work

Identity-provider setup, actual office coordinates, physical-device GPS testing, retention policy and load profiling remain deployment work. No salary deduction, payroll calculation, leave, payroll consumption, monthly locking or payslip generation is included in this handoff's implementation scope.
