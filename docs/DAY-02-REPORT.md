# LCAP HRMS - Day 2 Report

## Final Status

**PASS — final Day 2 repository/application audit, 7 September 2026.**

All results below come from checks executed during this completion audit. One frontend error-state defect was fixed and retested. This is not a production security certification or IIS deployment approval. No Geo Attendance, Check-In, Check-Out or other Day 3 features were added.

## Completed

Employee Master, required organisation assignments, reporting-manager self-reference, status/employment details, protected statutory/bank identifiers, JWT-compatible role/company access controls, employee screens and My Team are implemented.

The existing API/Application/Domain/Infrastructure separation, repository/unit-of-work, audit timestamps, soft-delete, REST response conventions and Angular layout remain in use. Day 1 backend regression tests passed. This audit changed the employee error presentation and strengthened existing SQL/API audit assertions; it added no business module or migration.

## Backend Build

**PASS**

- Full Release build: succeeded with 0 warnings and 0 errors; initial audit build 16.90 seconds.
- `dotnet test backend/LCAP.HRMS.sln -c Release --no-build -m:1`: 145 passed, 0 failed, 0 skipped: 35 persistence and 110 API tests. This includes 118 Day 1 cases and 27 Employee cases.
- Backend Release build rerun after the frontend fix: 8.05 seconds, 0 warnings, 0 errors.
- SQL integration harness rebuilt successfully: 14.53 seconds, 0 warnings, 0 errors.

Commands used single-node builds with build servers disabled to avoid the memory pressure encountered during initial Day 2 implementation. TRX evidence: each backend test project's `TestResults/day02-audit.trx`.

## Frontend Build

**PASS**

- `npm test`: 19 passed, 0 failed, both before and after the fix.
- `npm run build`: production build succeeded, both before and after the fix.
- Final production build: 13.613 seconds, no compiler or budget warnings.
- Initial bundle: 322.27 kB; Employee lazy chunk: 22.34 kB.

The existing frontend test command runs Node-based contract/helper/template tests; there is no comprehensive Angular TestBed component suite. Real browser checks supplemented those tests for rendering and interactions.

## Database/Migrations

**PASS** on local SQL Server 2022 using the actual EF Core SQL Server provider and Windows authentication.

Migration history includes all seven Day 1 migrations plus `20260907131530_EmployeeMaster`. Final EF model verification reported **no pending model changes**. The freshly generated `artifacts/day02/audit-migrations.sql` has the same SHA-256 as `database/scripts/EmployeeMaster.sql`.

Fresh isolated databases retained for inspection:

- `LCAP_HRMS_Day02_FinalAudit_20260907_a1`: guarded CREATE executed twice, all migrations applied twice, initial API/SQL suite, dependent-dropdown/masking/loading checks.
- `LCAP_HRMS_Day02_FinalAudit_20260907_a2`: fresh migration, regression after the frontend fix, final browser CRUD/My Team and SQL checks.

After replay on the first database: eight migration history entries, zero employee seeds, and unchanged Day 1 seed counts of 1 Company / 1 Branch / 5 Departments / 6 Designations / 1 Shift / 1 Work Location. Schema and permanent seed replay are idempotent. Disposable CRUD suites require a fresh database because deleted employee codes remain reserved; they are not permanent seed operations.

`dbo.Employees` has all requested fields, bounded nullable sensitive strings, SQL date columns, shared audit columns and IsDeleted. The following Employee foreign keys were queried directly: Company, Branch, Department, Designation, Shift, WorkLocation and ReportingManager. All seven are enabled, trusted and **NO_ACTION**; ReportingManager references Employees itself.

Verified indexes include unique CompanyId + EmployeeCode, ReportingManagerId, BranchId, DepartmentId, DesignationId, ShiftId, WorkLocationId, EmployeeStatus, and CompanyId + IsDeleted. Direct SQL attempts to duplicate a same-company code, self-reference as manager, or assign a nonexistent manager were rejected and rolled back. Retained soft-deleted rows and audit fields passed SQL assertions.

No production database or production configuration was changed. The temporary hosts were stopped, browser tab closed and generated access-token file removed. The read lock used for loading-state testing was released by rollback and completed successfully.

## Employee Master

**PASS**

Employee contains the requested organisation IDs, name/contact/personal fields, employment/separation dates, PAN/Aadhaar/UAN/ESIC, bank fields, profile URL, notes and IsActive. FullName is derived. Existing enum numeric values were preserved while adding the requested statuses and employment types.

Validation was executed for required/trimmed employee code, required first name/mobile/joining date, email, undefined enums, invalid PAN, invalid Aadhaar format, invalid IFSC, future DOB and employment-date ordering. Code uniqueness is case-insensitive and company-scoped, including soft-deleted records; the same code in another company was accepted.

Invalid Company/Branch, Department/Company, Designation/Company, Shift/Company and WorkLocation/Branch/Company assignments were rejected. A same-company branch with no matching location was also tested. Existing company is immutable on employee update; transfers require a future explicit workflow.

DELETE retains the row and updates audit fields. Deleted employees disappear from normal reads/list endpoints and cannot be selected as new managers. Referenced organisation records cannot be deleted or moved through application persistence while retained employee assignments exist.

## Reporting Manager

**PASS**

ReportingManagerId is optional and same-company. Tests rejected self-reporting, two-person cycles and three-person cycles with 409. Valid direct reports and upward reporting chains returned 200. Retained links participate in cycle checks. SQL Server employee writes use a serializable transaction and transaction-owned application lock.

Concurrent inverse updates were executed: one returned 200, the other 409, and subsequent hierarchy reads remained valid.

The required end-to-end scenario was executed with disposable `TEST-MGR-001` and `TEST-EMP-001`, assigned to LCAP / PATNA-HO / Human Resources / Executive / GENERAL / PATNA-OFFICE. TEST-EMP-001 reports to TEST-MGR-001. Attempting to make TEST-MGR-001 report back to TEST-EMP-001 was rejected. SQL subsequently confirmed TEST-MGR-001 still has no manager and TEST-EMP-001 still references it. The employee appeared in that manager's browser My Team screen.

Managers with retained reporting links must have them reassigned before deletion; the application does not silently orphan reports.

## Authorization

**PASS for the Employee module's implemented policies. Company-scoped authorization is not complete across the entire repository.**

Actual JWT bearer signature/issuer/audience/lifetime validation was exercised with a random test-only signing key and static issuer. Claims are `role`, `company_id` and `employee_id`. No product user IDs are hard-coded.

| Check | Executed result |
| --- | --- |
| Anonymous / malformed-token employee list | 401 |
| HRAdmin create / update in own company | 201 / 200 |
| HRUser or Employee modifying employee | 403 |
| Missing company claim for HRAdmin | 403 |
| Foreign-company detail / update | 403 |
| Foreign company requested in company-list route | 403 |
| Employee list / lookup / company list | Only caller's company records returned |
| Foreign branch requested as a list filter | Empty scoped result; foreign rows not returned |
| Manager own direct-report and My Team endpoints | 200; correct direct reports only |
| Manager requesting another employee's direct-report endpoint | 403 |
| Employee own profile / someone else's profile | 200 / 403 |
| HRUser / Manager / Employee sensitive details | Sensitive statutory/bank fields redacted |
| PayrollAdmin sensitive read | Allowed within company; writes denied by policy/tests |

SuperAdmin can access all companies. HRAdmin can read/write employees within the claimed company. HRUser has non-sensitive directory/profile reads; PayrollAdmin has sensitive reads; Managers can read subordinate profiles and their own direct-report list; Employees can read their own profile.

The older Day 1 Organisation endpoints still use their original authentication-only authorization. They are not covered by a claim that repository-wide role/company scoping is finished. External production identity-provider login/discovery/refresh was not tested.

## APIs

**PASS** for all requested routes:

| Method | Route |
| --- | --- |
| GET, POST | /api/employees |
| GET, PUT, DELETE | /api/employees/{id} |
| GET | /api/companies/{companyId}/employees |
| GET | /api/branches/{branchId}/employees |
| GET | /api/departments/{departmentId}/employees |
| GET | /api/employees/{id}/direct-reports |
| GET | /api/employees/{id}/reporting-chain |
| GET | /api/employees/lookup |
| GET | /api/employees/my-team |

The final suite recorded 79 HTTP operations: 26 × 200, 15 × 201, 3 × 204, 17 × 400, 2 × 401, 8 × 403, 2 × 404, 6 × 409. Additional concurrent updates, health, Swagger and test-token issuance are outside that recorded count. Swagger UI/JSON and anonymous GET /api/health succeeded.

List DTOs omit PAN, AadhaarNumber, BankAccountNumber, UAN and ESICNumber entirely. Detail access is role-scoped; sensitive responses are configured not to be cached. Queries use bounded paging and no-tracking projections where appropriate.

## Frontend Screens

**PASS**, using the current production bundle and actual SQL-backed API.

- Employee directory loaded real API data; Add, View and Edit completed successfully.
- Browser-created employee retained all organisation assignments and selected manager after edit. Create/edit success feedback appeared.
- Activate and Deactivate completed successfully.
- Switching Company changed Branch, Department, Designation and Shift options; foreign-company options were excluded.
- Switching Branch changed Work Location choices. The branch with no locations showed no location option; PATNA-HO offered Patna Office.
- Reporting-manager selection persisted, and edit options excluded the current employee.
- Profile rendered synthetic PAN as `******234F` and synthetic Aadhaar/bank values as `********0000`; the raw example PAN was absent from visible profile text.
- My Team loaded under a Manager JWT and displayed TEST-EMP-001 and the browser-created direct report under TEST-MGR-001, with View-only actions. Attendance remains a placeholder.
- Disconnected load failure displayed the session error and Retry loading action.
- Loading indicator was observed while a short read lock delayed the isolated database; the table loaded normally after rollback.
- Invalid-PAN form submission was rejected. After the fix, entered values stayed intact and the misleading reload action was absent.
- Browser error logs were empty.

Browser records are disposable synthetic fixtures in the isolated databases, not production seeds. Statutory values were supplied only to exercise masking and remain distinguishable from real employee information.

## Tests

**174 automated cases passed:** 145 backend, 19 frontend and 10 additional real SQL/JWT integration cases. Reruns are not counted twice. Browser and direct SQL assertions are additional evidence.

After fixing the frontend defect, the affected browser checks, all frontend tests, both builds and the full SQL-backed integration regression were rerun successfully. Backend product code was unchanged during this audit.

Audit improvements to reusable tests:

- `tests/day02/api-integration.test.mjs`: explicit tenant filtering on list/lookup/company routes, foreign-company update denial and foreign-branch row exclusion.
- `tests/day02/verify-database.sql`: direct unique-code and reporting-manager FK rejection tests with rollback.

Evidence: backend `TestResults/day02-audit.trx`, `artifacts/day02/http-evidence.json`, `audit-migrations.sql`, `audit-host.log`, `audit-retest-host.log` and associated stderr logs. Generated artifacts are ignored by version control. Reproduction instructions remain in [Day 2 tests](../tests/day02/README.md).

## Security Checks

**PASS within this repository/application audit's limits; no production security certification is implied.**

Source, configuration, scripts and documentation were reviewed, excluding dependencies/generated build output. Credential/private-key/password-bearing connection-string patterns produced zero matches. Production connection settings use Windows integrated authentication, encryption and a server placeholder; local certificate trust is limited to development/test settings.

PAN/12-digit candidate scanning found only the known synthetic fixtures in four test files. No real Aadhaar, PAN, bank, UAN or ESIC data was identified. Pattern scanning cannot establish ownership of an arbitrary identifier, so this is not a proof about external systems or private data outside the repository.

No employee production seeds exist. LCAP PAN/TAN/GST/PF/ESI and Patna coordinates remained NULL in direct SQL checks. Synthetic identifier values used for validation/masking tests were not written into production migration seeds.

Both audit API log sets and backend TRX logs had zero matches for the sensitive fixture values. API logs had no warning/error/unhandled-exception/500 matches; browser error logs were empty. Exception/validation handling avoids echoing submitted sensitive identifiers. No permanent credentials or signing keys were introduced; the test host uses an ephemeral key and its generated token file was removed.

## Bugs Found

One Day 2 frontend defect: validation/save errors used the same Retry loading action as initial-load errors. Selecting it could reload/reset the employee form and discard unsaved entries.

No additional backend or database defect was found by the executed checks. Earlier implementation fixes remain present and passed regression; they are not counted as new audit defects.

## Bugs Fixed

Added a dedicated `loadFailed` state in `frontend/lcap-hrms-web/src/app/employees/employees.ts` and conditioned the Retry loading button in `employees.html` on that state. Validation/save errors now leave the user in the form to correct and resubmit; true load failures retain the retry action.

Browser regression verified an invalid PAN produces the validation message without Retry loading, while the entered first name remains unchanged. The corrected form then saved and edited successfully. A disconnected directory still offered Retry loading. Both builds, frontend tests and the extended SQL/API regression passed after the fix.

## Known Issues

- External OIDC login/refresh/key rotation, production JWT mapping, IIS hosting and production HTTPS/SQL certificate setup were not exercised.
- The frontend retains the memory-only token-entry form and placeholder Administrator label.
- Company transfers and deletion of managers with retained reporting links need explicit workflows.
- IsActive and EmployeeStatus remain separate fields; profile activation maps to Active/Inactive. Full lifecycle transition rules are future work.
- Actual Patna coordinates and GENERAL half/full-day thresholds remain unset.

## Technical Debt

- Complete role/company policy rollout for Day 1 Organisation endpoints before broad production exposure.
- Organisational ownership is service-validated using separate SQL FKs, not composite ownership constraints. Direct SQL writes and concurrent master moves warrant stronger protection before high-volume use.
- Employee hierarchy writes currently use a global SQL application lock; company-level locking and scalable traversal can be considered when justified.
- Full PUT lacks optimistic row-version concurrency. Form lookups load all pages; larger datasets need incremental lookup search.
- Sensitive columns are not application-encrypted. Database/backup encryption and least-privilege access must be configured before real employee data; masking is not encryption.
- Frontend helper/template tests are supplemented by browser checks, not a complete Angular component suite.

## Day 3 Readiness

**YES** to begin the defined Day 3 development work on the validated Day 2 foundation. This does not authorize production rollout or imply Day 3 is implemented.

`docs/DAY-03-HANDOFF.md` was reviewed. Its scope remains geo-fenced attendance, check-in/out, GPS/distance/accuracy validation and Employee-to-WorkLocation enforcement. Its configuration and security decisions are still applicable. No unrelated Day 3 functionality was added to the handoff.

## Pending for Day 3

Geo-fenced Attendance, Check-In, Check-Out, GPS validation, distance calculation, accuracy validation and Employee-to-WorkLocation enforcement. Obtain actual coordinates and agree timezone/overnight shift, GPS accuracy/staleness, duplicate-request and eligibility rules before implementation. See [Day 3 handoff](DAY-03-HANDOFF.md).

## Build Result

| Area | Final audit result |
| --- | --- |
| Backend | PASS; final build 8.05 seconds, 0 warnings/errors |
| Frontend | PASS; final build 13.613 seconds, no compiler/budget warnings |
| Database | PASS; fresh migration/replay and SQL constraint checks |
| Employee CRUD | PASS; API and browser |
| Reporting Manager | PASS; hierarchy, cycles and My Team |
| Authorization | PASS for implemented Employee policies; repository-wide limitations disclosed |
| Tests | PASS; 174 automated cases plus browser/direct SQL checks |
| Ready for Day 3 | YES |

No unresolved failures remain in the executed Day 2 audit checks.
