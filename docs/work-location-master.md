# Work Location Master

Work locations belong to branches and carry the owning CompanyId. Entity, DTOs, service, repositories, EF mappings, and the six authenticated routes follow the existing master-data layers.

## Endpoints

| Method | Route | Success | Expected errors |
| --- | --- | --- | --- |
| GET | /api/work-locations | 200, array envelope | 400, 401 |
| GET | /api/work-locations/{id} | 200, location envelope | 401, 404 |
| GET | /api/branches/{branchId}/work-locations | 200, array envelope | 400, 401, 404 |
| POST | /api/work-locations | 201, envelope and Location | 400, 401, 404, 409 |
| PUT | /api/work-locations/{id} | 200, updated envelope | 400, 401, 404, 409 |
| DELETE | /api/work-locations/{id} | 204, no body | 401, 404 |

Lists accept skip >= 0 and take from 1 to 1000 (defaults 0 and 100), ordered by Id. They include inactive locations but hide deleted locations and locations whose company or branch is deleted. Branch-scoped lists return 404 for missing/deleted parents and an empty array for an existing branch without locations.

## Validation

- CompanyId and BranchId must be non-empty. The service requires an existing non-deleted branch whose CompanyId matches the request.
- LocationCode and LocationName are required. Codes are trimmed, uppercased, and unique within their branch, including deleted rows.
- Latitude and Longitude are nullable decimal(10,7). Latitude must be -90 through 90 and Longitude -180 through 180.
- Supply both coordinates or neither. API validation rejects more than seven decimal places instead of silently rounding.
- AllowedRadiusMeters is an integer greater than zero, defaulting to 100. Explicit zero is rejected, not replaced by the default.
- IsGeoFenceEnabled defaults to true. An explicit false is preserved.
- Country defaults to India. Address, City, State, and PinCode are optional bounded strings.

This module stores configuration only. Enabled=true with null coordinates is an incomplete configuration, not a usable geofence. Future attendance/location enforcement must require actual coordinates and must never substitute zero for missing coordinates. No geofence calculation or attendance logic is implemented here.

PUT replaces writable fields and may move a location to another branch/company pair after validation. Omitted coordinates become null, radius defaults to 100, and the geofence flag defaults to true. Clients cannot write identity, audit fields, or deletion state.

DELETE performs soft deletion. Branch/company deletion hides child locations without removing their physical rows. A branch with work-location records (including deleted records) cannot change company through Branch Master; this returns 409 to preserve ownership consistency. Empty branches remain movable. Reassign live locations through their API first; retained deleted locations require a separately designed administrative process.

## Storage and seed

dbo.WorkLocations has non-cascading foreign keys to Company and Branch, a company index, a unique (BranchId, LocationCode) index, and database checks for coordinate ranges/pairing, required code/name, and positive radius. Matching CompanyId to the branch's owner is an application rule; direct SQL must preserve it.

WorkLocationMaster follows DesignationMaster. Its Patna seed is:

| Field | Value |
| --- | --- |
| Company / Branch | LCAP / PATNA-HO |
| LocationCode / LocationName | PATNA-OFFICE / Patna Office |
| City / State / Country | Patna / Bihar / India |
| Latitude / Longitude | null / null |
| AllowedRadiusMeters | 100 |
| IsGeoFenceEnabled / IsActive | true / true |

Address and PinCode are also null. No coordinates or street address were invented. Seed ID and timestamp are fixed for deterministic migrations.

Generate the reviewed SQL script without applying it:

```powershell
dotnet ef migrations script 0 WorkLocationMaster --idempotent --project backend/LCAP.HRMS.Infrastructure --startup-project backend/LCAP.HRMS.Api --context ApplicationDbContext --output database/scripts/WorkLocationMaster.sql
```

The script includes all earlier migrations and skips those already recorded. No startup code migrates or seeds databases.

## Verification

Development Swagger documents all routes at http://localhost:5080/swagger. Run dotnet test backend/LCAP.HRMS.sln --configuration Release.

Tests cover null seed coordinates, bounds and precision, paired coordinates, radius defaults and rejection, explicit geofence disabling, CRUD, audit retention, company/branch matching, reassignment, duplicate codes, soft deletion, parent filtering, and Swagger. Synthetic boundary coordinates exist only in isolated test records; the Patna seed remains null. Persistence tests use disposable SQLite databases, with SQL Server model checks for decimal precision and defaults.
