# Architecture

LCAP.HRMS has one deployable API and one Angular SPA. Company, Branch, Department, Designation, Work Location, and Shift masters are backend modules of the modular monolith; the Angular frontend includes Organisation administration for all six masters with shared table/form components and a bearer-authenticated HTTP client.

| Layer | Responsibility | Project references |
| --- | --- | --- |
| API | REST endpoints, response contracts, auth, middleware, composition | Application, Infrastructure |
| Application | Use cases, DTOs and service abstractions when needed | Domain |
| Domain | Business rules and domain models when needed | None |
| Infrastructure | SQL Server persistence and integration implementations | Application, Domain |

Future modules should organize their use cases and domain types under consistent module folders in each layer. Keep module internals private where practical and communicate through explicit application contracts. Do not introduce direct controller-to-DbContext dependencies. Module ownership and database schemas should be decided when actual business requirements are introduced.

ApplicationDbContext is scoped through dependency injection. It discovers EF configurations in Infrastructure and uses the SQL Server provider with transient retry support. BaseEntity provides auditing and soft deletion; repository and commit interfaces live in Application, with implementations in Infrastructure. The context itself implements IUnitOfWork, avoiding a second unit-of-work class. CompanyService coordinates the company repository and commit interface; controllers own HTTP responses. CompanyMaster adds dbo.Companies and its LCAP seed after InitialFoundation. See [foundation usage](backend-foundation.md) and [Company Master](company-master.md).

ApiResponse<T> describes successful responses and error envelopes. HTTP status codes remain meaningful. TraceId correlates failures with server logs. The exception middleware never exposes exception messages or stack traces in the response.

BranchService uses branch and company repository interfaces with the shared unit of work. BranchMaster adds dbo.Branches, a required non-cascading Company relationship, and a composite company/code unique index. Branch filtering also hides rows whose company is deleted. See [Branch Master](branch-master.md).

GET /api/health is a liveness endpoint: it proves that the API can serve requests. It does not prove SQL Server or the identity provider is ready. A dependency readiness check can be added when real dependencies are provisioned.

Authentication is configured, but an identity provider must be supplied before protected APIs can work. Future endpoints are authenticated by default; use AllowAnonymous only when intentionally public.

DepartmentService validates same-company ancestry, rejects self-parenting and longer cycles, and prevents deletion while non-deleted children exist. DepartmentMaster adds the company-scoped unique code index and a composite self-referencing foreign key. See [Department Master](department-master.md).

DesignationService follows the same repository/commit pattern. DesignationMaster adds dbo.Designations, the required company relationship, a per-company code index, nullable Level, and IsManagerial=false. See [Designation Master](designation-master.md).

WorkLocationService validates matching company/branch ownership, nullable decimal coordinates, and radius values. WorkLocationMaster adds database checks, parent relationships, and the Patna office seed with null coordinates. Branches with retained location records cannot change company. See [Work Location Master](work-location-master.md).

ShiftService follows the existing repository/commit pattern. ShiftMaster adds time-only shift definitions, overnight support, company-scoped uniqueness, and the General Shift seed. See [Shift Master](shift-master.md).
