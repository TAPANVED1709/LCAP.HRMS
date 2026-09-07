# LCAP HRMS - Day 2 Handoff

## Target

Implement Employee Master and the following relationships:

- Reporting Manager → Employee (nullable self-reference).
- Employee → Company.
- Employee → Branch.
- Employee → Department.
- Employee → Designation.
- Employee → Shift.
- Employee → Work Location.

Do not implement attendance calculation or payroll processing as part of this target.

## Day 1 baseline

The .NET 8 modular monolith and Angular 21 Organisation screens are implemented and passed Day 1 integration testing. See [Day 1 report](DAY-01-REPORT.md).

Existing tables: Companies, Branches, Departments, Designations, Shifts and WorkLocations.
Latest migration: 20260907102112_ShiftMaster.
The SQL Server 2022 migration chain was executed twice on a separate local test database without duplicate seeds.

Seed organisation: LCAP, Bihar, India, INR. Branch: PATNA-HO. Departments: HR, FIN, SALES, OPS, MGMT. Designations: EXEC, SREXEC, TL, MGR, HRM, PAYADMIN. Shift: GENERAL, 09:30–18:30, grace 15. Patna Office coordinates and company statutory registration identifiers remain null.

## Reuse the current architecture

- Domain: add Domain/Employees/Employee.cs inheriting BaseEntity.
- Application: Employee create/update/response DTOs, IEmployeeService and EmployeeService. Follow existing explicit mapping and validation.
- Persistence: ApplicationDbContext, IEntityTypeConfiguration, scoped repositories and the existing direct DbContext implementation of IUnitOfWork. Do not introduce another transaction wrapper.
- API: Employee controller with bearer protection, standard ApiResponse envelope, XML Swagger comments and meaningful 200/201/204/400/404/409 responses.
- Frontend: enable the Employees navigation item and add employee routes. Reuse the table, reactive form controls, API/session infrastructure, loading/error/success states and confirmed deletion.
- Migration: create a new EmployeeMaster migration; preserve all seven existing migrations and their deterministic seeds.

Useful source locations:

- backend/LCAP.HRMS.Domain/Common/BaseEntity.cs
- backend/LCAP.HRMS.Application/Shifts/
- backend/LCAP.HRMS.Infrastructure/Persistence/ApplicationDbContext.cs
- backend/LCAP.HRMS.Infrastructure/Persistence/Configurations/
- backend/LCAP.HRMS.Api/Controllers/
- frontend/lcap-hrms-web/src/app/shared/
- frontend/lcap-hrms-web/src/app/organisation/
- tests/day01/README.md

## Relationship rules to implement

1. Require a valid, non-deleted Company. Validate company ownership for every selected master.
2. A Work Location must belong to the selected Branch and Company.
3. Reporting Manager must belong to the same company, cannot be the employee itself, and cannot create a reporting cycle. A null manager represents the top of a reporting chain.
4. Use restrictive foreign keys and soft-delete. Define how historical employee records are displayed when referenced masters become inactive or deleted; avoid hiding employees accidentally through required-navigation query filters.
5. Decide whether employees can change company, and what should happen to all dependent selections. Do not silently move them across companies.
6. Clear incompatible frontend selections when Company or Branch changes. Reject inconsistent combinations again on the server.
7. Decide required versus optional assignment fields before enforcing non-null database columns.
8. Decide whether selecting inactive masters is allowed; retain existing inactive assignments for historical records where needed.

## Business decisions to confirm

The user has not supplied the complete employee field specification. Confirm rather than invent:

- Employee code format, generation and uniqueness scope.
- Required personal/contact/employment fields.
- Required assignment fields and joining/exit-date validation.
- Employee lifecycle and employment-type transitions.
- Single current Shift versus effective-dated assignment history.
- Reassignment rules, reporting-manager eligibility and cross-company transfers.
- Whether statutory/bank details belong in the first Employee Master release and their access controls.

Existing EmployeeStatus and EmploymentType enums are placeholders. Keep persisted enum numeric values stable unless an explicit migration is planned. RecordStatus and IsDeleted have different meanings.

Do not fabricate employee personal details, PAN/TAN/GST values, bank information or office coordinates. Any test-only data must be confined to isolated test databases.

## Acceptance tests

- Employee CRUD, validation, audit timestamps, soft-delete and duplicate employee-code rejection.
- Correct Company/Branch/Department/Designation/Shift/Work Location relationships.
- Missing/deleted/wrong-company masters and mismatched Work Location/Branch rejected.
- Self-manager and indirect reporting cycles rejected.
- Root employee with null manager supported.
- Activate/deactivate without losing optional fields.
- Frontend dependent selectors, full-record updates, error display and delete confirmation.
- SQL Server migration execution and idempotent script re-run without duplicate seeds.
- Existing Day 1 suites remain green.
- Final backend and frontend production builds pass without functional warnings.

## Operational limits carried forward

Production identity-provider sign-in/refresh, role/company-level access rules, IIS deployment and trusted production certificates remain to be configured and tested. The current UI profile label is a placeholder, not an authorization grant.

Master list search/paging is currently client-side. Full-record PUT operations have no optimistic concurrency token. Plan improvements if employee data volumes or concurrent editing require them.

Day 1 tested the real JWT middleware using ephemeral test keys; it did not test an external identity provider. The isolated test host must never be deployed as the production API.

General Shift half/full-day thresholds are unconfigured (null). Company payroll and salary-payment day seeds are both 1. Confirm actual business settings before using them in later processing.

## Start Day 2

Read DAY-01-REPORT.md, confirm the employee field requirements, then implement the Employee slice end to end. Use a new isolated database for integration tests; never auto-apply migrations to production.
