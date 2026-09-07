# Shift Master

Shift belongs to Company and inherits audit fields and soft deletion from BaseEntity.
The Domain, Application, Infrastructure, and API layers follow the existing master-data pattern.

## API

All operations require a bearer token. Swagger is available in Development.

| Method | Route | Success |
| --- | --- | --- |
| GET | /api/shifts | 200 |
| GET | /api/shifts/{id} | 200 |
| GET | /api/companies/{companyId}/shifts | 200 |
| POST | /api/shifts | 201 with Location |
| PUT | /api/shifts/{id} | 200 |
| DELETE | /api/shifts/{id} | 204 |

Lists accept skip (default 0) and take (default 100, maximum 1000).
Invalid requests return 400, missing/deleted records or companies return 404, and duplicate codes return 409.
DELETE retains the row and updates audit fields. Deleted companies hide their shifts.
Codes are trimmed and uppercased and remain reserved after deletion.

## Time and threshold semantics

StartTime and EndTime are required TimeOnly values stored as SQL Server time(7).
Send local wall-clock times, for example "09:30:00" and "18:30:00"; no UTC conversion is applied.
Midnight "00:00:00" is valid. Earlier EndTime means the following day (22:00 to 06:00 is eight hours).
Such shifts automatically receive IsNightShift=true. Explicit true is also allowed for a night shift that does not cross midnight.
Equal times are rejected to avoid ambiguity between zero and 24 hours.

GracePeriodMinutes defaults to zero and must be non-negative.
MinimumHalfDayMinutes and MinimumFullDayMinutes are nullable because no business thresholds were supplied.
Supplied thresholds must be non-negative and half-day cannot exceed full-day when both are set.
PUT replaces writable details and clears omitted thresholds.
IsActive defaults true. Audit fields and IsDeleted are managed by the backend.
This module stores shift definitions; attendance classification and payroll calculations are not implemented.

Example create body:

```json
{
  "companyId": "ec8af472-df65-43d4-9abf-9378e553e455",
  "shiftCode": "NIGHT",
  "shiftName": "Night Shift",
  "startTime": "22:00:00",
  "endTime": "06:00:00",
  "gracePeriodMinutes": 15,
  "minimumHalfDayMinutes": null,
  "minimumFullDayMinutes": null,
  "isNightShift": true,
  "isActive": true
}
```

## Seed and migration

LCAP receives GENERAL / General Shift, 09:30 to 18:30, grace 15, active true, night false.
Both attendance thresholds remain null.

ShiftMaster creates dbo.Shifts, its company foreign key, unique company/code index, validation checks, and seed.
Review database/scripts/ShiftMaster.sql for the full idempotent deployment script.
No database is updated automatically.

## Files

- Domain/Shifts/Shift.cs
- Application/Shifts: repository interface, service interface and implementation, DTOs
- Infrastructure/Persistence: ShiftConfiguration, ShiftRepository, ShiftSeedData, migration and snapshot
- Api/Controllers/ShiftsController.cs
- Api.Tests/ShiftApiTests.cs

Paths above are relative to their backend projects. Dependency injection, Company navigation,
ApplicationDbContext filters/conflict handling, and Swagger security registration are also updated.
