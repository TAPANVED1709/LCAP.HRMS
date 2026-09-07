# LCAP.HRMS

Enterprise HRMS: a modular monolith with an ASP.NET Core Web API targeting .NET 8, EF Core 8 and SQL Server 2022, plus an Angular 21 frontend. Shared persistence, auditing, soft deletion, Company, Branch, Department, Designation, Work Location, and Shift masters are implemented. Migrations include the LCAP organization seeds and a Patna office with null coordinates. Login flows, attendance, geofence enforcement, and payroll processing are not implemented.

## Prerequisites

- .NET 8 SDK (a newer SDK capable of targeting net8.0 also works), and the ASP.NET Core 8 runtime.
- Node.js 24 LTS and npm; Angular 21 supports Node ^20.19, ^22.12, or ^24.0. See [Angular compatibility](https://angular.dev/reference/versions).
- SQL Server 2022, needed for the master-data APIs. The health endpoint does not connect to SQL Server.
- For production: Windows Server with IIS and the .NET 8 Hosting Bundle.

## Local backend setup

Run from the repository root in PowerShell:

```powershell
dotnet restore backend/LCAP.HRMS.sln
dotnet tool restore
dotnet build backend/LCAP.HRMS.sln --configuration Release
dotnet run --project backend/LCAP.HRMS.Api --launch-profile http
```

- API health: http://localhost:5080/api/health
- Swagger UI (Development only): http://localhost:5080/swagger
- OpenAPI: http://localhost:5080/swagger/v1/swagger.json

Company endpoints are available at `/api/companies` and require a valid bearer token. See [Company Master](docs/company-master.md) for requests, validation, seed values, migrations, and response codes. Apply reviewed migrations to an explicitly configured local database before calling these persistence endpoints; health still requires no database.

Branch endpoints are available at `/api/branches` and `/api/companies/{companyId}/branches`. See [Branch Master](docs/branch-master.md) for company relationships, state defaults, per-company code uniqueness, and soft-delete behavior.

Department endpoints are available at `/api/departments` and `/api/companies/{companyId}/departments`. See [Department Master](docs/department-master.md) for same-company hierarchy validation, deletion rules, and the five LCAP department seeds.

Designation endpoints are available at `/api/designations` and `/api/companies/{companyId}/designations`. See [Designation Master](docs/designation-master.md) for optional Grade/Level, managerial defaults, and the six LCAP seeds.

Work-location endpoints are available at `/api/work-locations` and `/api/branches/{branchId}/work-locations`. See [Work Location Master](docs/work-location-master.md) for coordinate validation, defaults, ownership checks, and the null-coordinate Patna seed.

For local HTTPS, run `dotnet dev-certs https --trust`, then use `--launch-profile https`. The HTTPS address is https://localhost:7080. Development permits HTTP; production enables HTTPS redirection and HSTS.

Example health response (trace and time vary):

```json
{
  "success": true,
  "message": "Request completed successfully.",
  "data": { "status": "Healthy", "timestampUtc": "2026-09-07T00:00:00+00:00" },
  "traceId": "request-trace-id",
  "errors": []
}
```

## Configuration

Base configuration is in `backend/LCAP.HRMS.Api/appsettings.json`; Development overrides are in `appsettings.Development.json`.

The base SQL connection is a placeholder. Development defaults to SQL Server on localhost using Windows authentication. Override it for your instance before using persistence:

```powershell
$env:ConnectionStrings__DefaultConnection = 'Server=localhost;Database=LCAP_HRMS;Integrated Security=True;Encrypt=True;TrustServerCertificate=True;'
```

TrustServerCertificate=True is only a local development convenience. Production uses a trusted SQL Server certificate and TrustServerCertificate=False. Do not commit credentials. Supply production settings through secured deployment configuration or environment variables.

JWT bearer validation is registered for an external OpenID Connect/OAuth identity provider. Set `Jwt__Authority` to its HTTPS authority URL and `Jwt__Audience` to the API audience before using protected endpoints. Signing keys are obtained from that provider; issuer, audience, signature, and expiry are validated. The placeholder authority does not issue tokens. No local authentication business module is included.

The fallback authorization policy requires authentication. Health is explicitly anonymous. Development Swagger is publicly accessible; Swagger is disabled in production. HTTP errors and validation failures use the response envelope. Unhandled exceptions return a generic 500 response with a trace ID; details are logged server-side.

CORS allows `http://localhost:4200` in Development. Production allows no cross-origin clients until `Cors__AllowedOrigins__0` (and further indexed entries) is configured. Set `AllowedHosts` to your production API hostname.

## Local frontend setup

In another terminal:

```powershell
cd frontend/lcap-hrms-web
npm ci
npm start
```

Open http://localhost:4200. Settings > Organisation includes Company, Branch, Department, Designation, Shift and Work Location screens with searchable tables, validated forms and API integration. The dev server proxies /api/** to http://localhost:5080. Open Profile > API connection to supply a valid bearer token. See [frontend setup](frontend/lcap-hrms-web/README.md) and [verification](docs/frontend-organisation.md). The local proxy is not included in production.

```powershell
npm run build
```

Production files are generated in `frontend/lcap-hrms-web/dist/lcap-hrms-web/browser`.

## Architecture

```text
API ------------> Application ----> Domain
 |                     ^
 +--> Infrastructure --+---------> Domain
```

The API is the composition root and owns HTTP contracts, authentication, and middleware. Application owns use cases and service abstractions. Domain remains free of infrastructure dependencies. Infrastructure owns EF Core and external integrations.

ApplicationDbContext applies shared BaseEntity mappings, UTC audit timestamps, and soft-delete filters. It directly implements IUnitOfWork so application services can commit changes from multiple repositories together. InitialFoundation is the baseline; the Company, Branch, Department, Designation, Work Location, and Shift migrations add the current schema and seeds. No database is created or migrated automatically. See [foundation usage](docs/backend-foundation.md) and [architecture notes](docs/architecture.md).

After entities and mappings are introduced, create and review migrations explicitly:

```powershell
dotnet ef migrations add InitialCreate --project backend/LCAP.HRMS.Infrastructure --startup-project backend/LCAP.HRMS.Api --output-dir Persistence/Migrations
dotnet ef migrations script --idempotent --project backend/LCAP.HRMS.Infrastructure --startup-project backend/LCAP.HRMS.Api --output database/scripts/migrations.sql
```

Use reviewed SQL scripts for production deployment. `database/scripts/ShiftMaster.sql` includes all seven migrations; [seed documentation](database/seeds/README.md) explains the master-data seeds.

Run the backend foundation tests (isolated SQLite databases; no SQL Server connection required):

```powershell
dotnet test backend/LCAP.HRMS.sln --configuration Release
```

## Deployment and verification

See [IIS deployment instructions](deployment/README.md) and [verification results](docs/verification.md).

```text
LCAP.HRMS/
├── backend/
│   ├── LCAP.HRMS.Api/
│   ├── LCAP.HRMS.Application/
│   ├── LCAP.HRMS.Domain/
│   ├── LCAP.HRMS.Infrastructure/
│   ├── LCAP.HRMS.Infrastructure.Tests/
│   ├── LCAP.HRMS.Api.Tests/
│   └── LCAP.HRMS.sln
├── frontend/
│   └── lcap-hrms-web/
├── database/
│   ├── scripts/
│   └── seeds/
├── docs/
├── deployment/
├── .config/dotnet-tools.json
├── .gitignore
└── README.md
```

Shift endpoints are available at /api/shifts and /api/companies/{companyId}/shifts. See [Shift Master](docs/shift-master.md) for overnight time semantics and the General Shift seed.

## Day 1 integration

See [Day 1 report](docs/DAY-01-REPORT.md), [Day 2 handoff](docs/DAY-02-HANDOFF.md), and [repeatable integration checks](tests/day01/README.md).
