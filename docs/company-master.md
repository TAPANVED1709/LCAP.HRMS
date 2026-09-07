# Company Master

The module provides the Company entity, create/update/response DTOs, application service, EF mapping and repository, and authenticated REST endpoints. It stores company details and scheduling settings only; no attendance, salary calculation, or payroll processing is implemented.

## Endpoints

| Method | Route | Success | Expected errors |
| --- | --- | --- | --- |
| GET | /api/companies?skip=0&take=100 | 200, response envelope containing an array | 400, 401 |
| GET | /api/companies/{id} | 200, company response envelope | 401, 404 |
| POST | /api/companies | 201, company envelope and Location header | 400, 401, 409 |
| PUT | /api/companies/{id} | 200, updated company envelope | 400, 401, 404, 409 |
| DELETE | /api/companies/{id} | 204, no response body | 401, 404 |

All endpoints require a bearer token from the configured identity provider. No module-specific role policy is defined yet. Requests use the standard ApiResponse envelope, except 204 responses. Unexpected failures produce a generic 500 envelope.

Lists hide soft-deleted rows but include inactive companies. Paging accepts skip >= 0 and take from 1 to 1000, defaulting to 100; results are ordered by Id. A missing or deleted company returns 404. Repeated deletion also returns 404.

PUT replaces all writable fields. Omitted optional fields are cleared; omitted defaulted fields take their request defaults. ID, creation/update audit fields, and IsDeleted are not writable through the DTOs. DELETE preserves the physical row and stamps deletion audit data using the shared foundation.

## Validation and uniqueness

CompanyCode and CompanyName are required and reject whitespace-only values. Codes are trimmed and normalized to uppercase. A case-insensitive SQL Server unique index enforces uniqueness across all rows, including soft-deleted rows. The service checks before saving; SQL Server unique-key errors are translated into 409 as a concurrency backstop.

Country defaults to India. State is free text and accepts Bihar. PayrollCurrency defaults to INR and must contain three letters. Both scheduling day fields are required and must be 1–31; omission produces 0 and fails validation. These integers are configuration only; handling shorter months belongs to future payroll requirements.

String lengths are bounded to match the EF columns. LogoUrl, if provided, must pass URL validation; it is stored as text and never fetched. Statutory identifiers are optional and bounded, without pretending to verify their registration or legal validity. No PAN, TAN, or GSTIN values are fabricated.

## Example create request

```json
{
  "companyCode": "BRANCH",
  "companyName": "Branch Company",
  "state": "Bihar",
  "country": "India",
  "payrollCurrency": "INR",
  "payrollDay": 1,
  "salaryPaymentDay": 7,
  "isActive": true
}
```

## Database and seed

CompanyMaster adds dbo.Companies, bounded columns, the unique CompanyCode index, required code/name checks, and day-range check constraints. It includes the LCAP seed with Bihar, India, INR, active status, and day 1 for both scheduling fields. All unspecified statutory identifiers remain null.

InitialFoundation remains the baseline migration. CompanyMaster follows it. Generate the reviewed deployment script from the repository root:

```powershell
dotnet ef migrations script 0 CompanyMaster --idempotent --project backend/LCAP.HRMS.Infrastructure --startup-project backend/LCAP.HRMS.Api --context ApplicationDbContext --output database/scripts/CompanyMaster.sql
```

For an explicitly chosen local development database only, apply migrations after configuring its connection:

```powershell
dotnet ef database update --project backend/LCAP.HRMS.Infrastructure --startup-project backend/LCAP.HRMS.Api --context ApplicationDbContext
```

No database update is performed by application startup. Production should use a reviewed deployment script and separate deployment credentials.

## Swagger and tests

Run the API in Development and open http://localhost:5080/swagger. All five operations include summaries, behavior notes, request schemas with validation limits, response codes, and bearer security requirements. Use Authorize with a valid identity-provider token. OpenAPI JSON is available at /swagger/v1/swagger.json. Swagger remains disabled in Production.

Run dotnet test backend/LCAP.HRMS.sln --configuration Release. Tests cover real HTTP routing, envelopes, validation, authentication, CRUD, seed contents, duplicate and deleted-code conflicts, audit persistence, global filters, database constraints, and generated Swagger. The HTTP tests use a test-only authentication handler and isolated SQLite databases; no production authentication bypass is included. SQL Server connectivity and live identity-provider JWTs require deployment-specific verification.
