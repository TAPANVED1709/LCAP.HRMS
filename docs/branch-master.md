# Branch Master

Branch Master stores a company's branch details. The entity inherits BaseEntity for identity, UTC auditing, and soft deletion. Domain contains the entity, Application owns the DTOs and service, Infrastructure owns persistence and seeding, and the API owns HTTP behavior.

## Endpoints

All routes require a bearer token from the configured identity provider.

| Method | Route | Success | Expected errors |
| --- | --- | --- | --- |
| GET | /api/branches | 200, array envelope | 400, 401 |
| GET | /api/branches/{id} | 200, branch envelope | 401, 404 |
| GET | /api/companies/{companyId}/branches | 200, array envelope | 400, 401, 404 |
| POST | /api/branches | 201, branch envelope and Location header | 400, 401, 404, 409 |
| PUT | /api/branches/{id} | 200, branch envelope | 400, 401, 404, 409 |
| DELETE | /api/branches/{id} | 204, no body | 401, 404 |

Lists accept skip >= 0 and take from 1 to 1000, defaulting to 0 and 100. They are ordered by Id and include inactive branches. An existing company without branches returns an empty array; a missing or deleted company returns 404 on the company-scoped route.

## Rules

- CompanyId must be a non-empty GUID and refer to a non-deleted company. Inactive companies may own branches.
- BranchCode and BranchName are required and cannot be whitespace-only. Codes are trimmed and uppercased.
- A case-insensitive unique index on (CompanyId, BranchCode) enforces code uniqueness within each company. Codes can repeat across companies. Soft-deleted branch codes remain reserved within their company.
- A blank or omitted State copies the current company State on create or PUT. Explicit State values are retained. The copied value is stored; later changes to Company.State do not automatically update branches.
- Country defaults to India. If explicitly supplied, it must be nonempty.
- Optional Email and Phone values are validated; strings have limits aligned with database columns.
- IsHeadOffice is a flag. No rule limiting a company to one head office was requested or added.
- PUT replaces writable fields and permits changing CompanyId to another existing company, with uniqueness checked in the target company. Omitted optional fields are cleared, except State's documented fallback.
- Once a branch has work-location records, including soft-deleted records, changing its company returns 409. Empty branches remain movable. This keeps retained location CompanyId values consistent; see [Work Location Master](work-location-master.md).
- Identity, audit fields, and IsDeleted cannot be set through the request DTOs.

Deleting a branch updates IsDeleted and update audit fields without removing its row. Deleted branches return 404 and disappear from lists. Deleting a company hides its branches through a combined global query filter; their rows remain intact and are not cascade-deleted or independently marked deleted. There is no restore or purge endpoint.

## Relationship and indexes

dbo.Branches has a required foreign key to dbo.Companies with Restrict delete behavior. Company.Branches and Branch.Company expose the relationship. UX_Branches_CompanyId_BranchCode is both the unique constraint and an index whose leading CompanyId supports company-scoped queries. SQL Server retains the foundation's PascalCase columns, GUID primary key, and UTC datetimeoffset audit timestamps.

The service checks duplicate codes before saving. SQL Server duplicate-key errors provide the concurrency backstop and are translated into a 409 response. Missing/deleted parents are checked by the service; the foreign key prevents references to nonexistent company rows.

## Seed and migration

BranchMaster follows CompanyMaster and inserts PATNA-HO (Patna Head Office), owned by the existing LCAP company. It uses Patna, Bihar, India, IsHeadOffice=true, and IsActive=true. Address lines, PinCode, Email, and Phone remain null because none were supplied. The seed uses fixed IDs and a fixed audit timestamp for deterministic migrations.

Generate a reviewable script without applying it:

```powershell
dotnet ef migrations script 0 BranchMaster --idempotent --project backend/LCAP.HRMS.Infrastructure --startup-project backend/LCAP.HRMS.Api --context ApplicationDbContext --output database/scripts/BranchMaster.sql
```

The script includes InitialFoundation, CompanyMaster, and BranchMaster; already-recorded migrations are skipped. Application startup does not migrate or seed a database automatically.

Example create body (CompanyId below is the deterministic LCAP seed ID):

```json
{
  "companyId": "ec8af472-df65-43d4-9abf-9378e553e455",
  "branchCode": "PATNA-02",
  "branchName": "Patna Branch",
  "city": "Patna",
  "country": "India",
  "isHeadOffice": false,
  "isActive": true
}
```

State inherits Bihar from LCAP in this example.

## Verification

Swagger in Development documents all six routes, schemas, status codes, and bearer security at http://localhost:5080/swagger. Run dotnet test backend/LCAP.HRMS.sln --configuration Release for HTTP and persistence tests. Tests use isolated SQLite databases and a test-only authentication handler; SQL Server deployment and real identity-provider tokens require environment-specific verification.
