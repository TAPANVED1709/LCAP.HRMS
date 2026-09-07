# Department Master

Departments belong to one company and can form a hierarchy through nullable ParentDepartmentId. The entity inherits BaseEntity; DTOs and DepartmentService live in Application, mappings and repositories in Infrastructure, and HTTP behavior in the API.

## Endpoints

All routes require a bearer token. Successful payloads use the standard ApiResponse envelope, except DELETE's empty 204 response.

| Method | Route | Success | Expected errors |
| --- | --- | --- | --- |
| GET | /api/departments | 200, array | 400, 401 |
| GET | /api/departments/{id} | 200, department | 401, 404 |
| GET | /api/companies/{companyId}/departments | 200, array | 400, 401, 404 |
| POST | /api/departments | 201, department and Location | 400, 401, 404, 409 |
| PUT | /api/departments/{id} | 200, department | 400, 401, 404, 409 |
| DELETE | /api/departments/{id} | 204, no body | 401, 404, 409 |

Lists use skip >= 0 and take from 1 to 1000, defaulting to 0 and 100, ordered by Id. They include inactive departments and exclude deleted departments or departments whose company is deleted. An existing company without departments returns an empty array; a missing or deleted company returns 404.

## Validation and hierarchy

CompanyId must reference a non-deleted company. DepartmentCode and DepartmentName are required and cannot be whitespace-only. Codes are trimmed, uppercased, and unique within the company, including soft-deleted records. Different companies may use the same code. The SQL Server unique index is the backstop for concurrent duplicate writes.

ParentDepartmentId may be null for a root department. If supplied, it must identify an existing non-deleted department in the same company. The service walks its ancestors and rejects self-parenting or a cycle with HTTP 400. A database check constraint independently prevents direct self-parenting, and a composite foreign key enforces parent/company consistency. Missing or deleted parents return 404.

CompanyId is immutable after creation because it participates in the composite hierarchy key. Changing it through PUT returns 400. PUT replaces all writable fields; null ParentDepartmentId removes the parent relationship, and omitted Description clears the description. Identity, audit fields, and IsDeleted are not writable through request DTOs.

DELETE soft-deletes a department. If it has non-deleted children, it returns 409: reassign or delete those children first. Both active and inactive children count. There is no cascading soft deletion, restoration, or subtree-move endpoint. Deleting a company hides its departments while retaining their physical rows.

Hierarchy validation and child-deletion checks run in the application service; database constraints enforce direct self-parenting and same-company references. Longer-cycle detection is not a database constraint. Direct SQL must not bypass the application hierarchy rules.

## Storage

- dbo.Departments, GUID Id, required CompanyId, and the shared UTC audit columns.
- Required DepartmentCode (50 characters) and DepartmentName (200); optional Description (1000).
- Case-insensitive UX_Departments_CompanyId_DepartmentCode.
- Alternate key (CompanyId, Id) and self-reference (CompanyId, ParentDepartmentId), preventing cross-company parents.
- Indexes for parent lookup; foreign keys use Restrict/NO ACTION.
- Standard shared soft-delete and company-deletion query filters.

## Seed and migration

DepartmentMaster follows BranchMaster and adds five active root departments for LCAP:

| Code | Name |
| --- | --- |
| HR | Human Resources |
| FIN | Finance |
| SALES | Sales |
| OPS | Operations |
| MGMT | Management |

Descriptions and ParentDepartmentId are null. IDs and seed audit timestamps are fixed for deterministic migrations. No hierarchy was inferred for these five departments.

Generate the full reviewable deployment script:

```powershell
dotnet ef migrations script 0 DepartmentMaster --idempotent --project backend/LCAP.HRMS.Infrastructure --startup-project backend/LCAP.HRMS.Api --context ApplicationDbContext --output database/scripts/DepartmentMaster.sql
```

This includes the preceding migrations; already-recorded ones are skipped. No startup code migrates or seeds a database automatically.

Example create request using LCAP's seeded company ID:

```json
{
  "companyId": "ec8af472-df65-43d4-9abf-9378e553e455",
  "departmentCode": "IT",
  "departmentName": "Information Technology",
  "description": "Technology support",
  "parentDepartmentId": null,
  "isActive": true
}
```

Swagger in Development documents all six endpoints at http://localhost:5080/swagger.

## Verification

Run dotnet test backend/LCAP.HRMS.sln --configuration Release. HTTP tests cover seeds, CRUD, defaults, audit preservation, uniqueness, hierarchy validation and reparenting, child-deletion conflicts, missing companies/parents, paging, authentication, and Swagger. Persistence tests independently check unique codes, foreign keys, cross-company parents, and the self-parent constraint using isolated SQLite databases. SQL Server script generation is verified without applying it to a database.
