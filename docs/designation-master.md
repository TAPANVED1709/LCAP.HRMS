# Designation Master

Designations belong to companies and inherit the shared identity, UTC audit fields, and soft-delete behavior. The module includes create/update/response DTOs, IDesignationService and DesignationService, a repository, EF mappings, and authenticated REST endpoints.

## Endpoints

| Method | Route | Success | Expected errors |
| --- | --- | --- | --- |
| GET | /api/designations | 200, array envelope | 400, 401 |
| GET | /api/designations/{id} | 200, designation envelope | 401, 404 |
| GET | /api/companies/{companyId}/designations | 200, array envelope | 400, 401, 404 |
| POST | /api/designations | 201, envelope and Location | 400, 401, 404, 409 |
| PUT | /api/designations/{id} | 200, updated envelope | 400, 401, 404, 409 |
| DELETE | /api/designations/{id} | 204, no body | 401, 404 |

All routes require a bearer token. Lists accept skip >= 0 and take from 1 to 1000 (defaults 0 and 100) and are ordered by Id. Inactive rows are included; deleted designations and designations belonging to deleted companies are hidden. An existing company without designations returns an empty array; a missing or deleted company returns 404.

## Rules and types

- CompanyId must be a non-empty GUID referencing a non-deleted company.
- DesignationCode and DesignationName are required and reject whitespace-only strings.
- Codes are trimmed, uppercased, and unique within a company. The same code is allowed in another company. Soft-deleted codes stay reserved.
- Grade is optional text up to 50 characters. Description is optional text up to 1000 characters. Code and name limits are 50 and 200.
- Level is a nullable 32-bit integer. There is no additional minimum, maximum, or grade/level relationship rule.
- IsManagerial defaults to false in the request, entity, and database. It is independent of the designation title.
- IsActive defaults to true in the entity and request.

PUT replaces all writable fields. It can reassign a designation to an existing company after checking code availability there. Omitted Grade, Description, and Level become null, and omitted IsManagerial becomes false. Clients cannot write Id, audit fields, or IsDeleted. DELETE preserves the row and stamps update audit values; repeated deletion returns 404.

## Database and seed

dbo.Designations uses a required company foreign key with Restrict/NO ACTION and a case-insensitive unique index on (CompanyId, DesignationCode). The leading CompanyId supports company-scoped queries. SQL Server duplicate-key failures are mapped to 409 if a concurrent write bypasses the service precheck.

DesignationMaster follows DepartmentMaster and seeds six active LCAP designations:

| Code | Name |
| --- | --- |
| EXEC | Executive |
| SREXEC | Senior Executive |
| TL | Team Leader |
| MGR | Manager |
| HRM | HR Manager |
| PAYADMIN | Payroll Admin |

All seed Grade, Level, and Description values are null. IsManagerial is false for all six, following the requested default without inferring policy from titles. Seed IDs and audit timestamps are fixed for deterministic migrations.

Generate the full idempotent deployment script without applying it:

```powershell
dotnet ef migrations script 0 DesignationMaster --idempotent --project backend/LCAP.HRMS.Infrastructure --startup-project backend/LCAP.HRMS.Api --context ApplicationDbContext --output database/scripts/DesignationMaster.sql
```

The script includes all prior migrations and skips those already recorded. Startup does not migrate or seed a database automatically.

Example body using LCAP's seeded company ID:

```json
{
  "companyId": "ec8af472-df65-43d4-9abf-9378e553e455",
  "designationCode": "ANALYST",
  "designationName": "Analyst",
  "description": null,
  "grade": null,
  "level": null,
  "isManagerial": false,
  "isActive": true
}
```

## Swagger and verification

Development Swagger documents all six operations at http://localhost:5080/swagger, including schemas, response codes, and bearer authentication.

Run dotnet test backend/LCAP.HRMS.sln --configuration Release. Tests exercise HTTP CRUD, nullable Level, default/explicit/reset managerial flags, code uniqueness, company reassignment, soft deletion, parent-company filtering, validation, seeds, authentication, paging, and Swagger. Database checks verify uniqueness and company foreign keys; a SQL Server model check verifies column types, nullability, and defaults. Persistence tests use isolated SQLite databases; no configured SQL Server or production database is updated.
