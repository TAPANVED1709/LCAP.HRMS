# Master data seeds

LCAP is seeded through the CompanyMaster EF migration, using Infrastructure/Persistence/Seeds/CompanySeedData.cs. Do not run a second insert script.

| Field | Value |
| --- | --- |
| CompanyCode | LCAP |
| CompanyName | LCAP |
| State | Bihar |
| Country | India |
| PayrollCurrency | INR |
| PayrollDay | 1 |
| SalaryPaymentDay | 1 |
| IsActive | true |

The user allowed days 1, 7, 15, and 31; the seed uses 1 for both day fields. PAN, TAN, GSTIN, PF/ESI registration numbers, and other unspecified optional fields are null.

The seed has a fixed GUID and fixed UTC creation timestamp for deterministic migrations. Applying the reviewed migration inserts the row; application startup does not seed or migrate automatically.

## Patna Head Office

BranchMaster inserts the branch through Infrastructure/Persistence/Seeds/BranchSeedData.cs, referencing the existing LCAP seed ID.

| Field | Value |
| --- | --- |
| Company | LCAP |
| BranchCode | PATNA-HO |
| BranchName | Patna Head Office |
| City | Patna |
| State | Bihar |
| Country | India |
| IsHeadOffice | true |
| IsActive | true |

Unspecified address/contact fields remain null. The branch ID and audit timestamp are fixed for deterministic migrations.

## Departments

DepartmentMaster inserts five active root departments for LCAP through Infrastructure/Persistence/Seeds/DepartmentSeedData.cs. Descriptions and parent IDs are null; each seed has a fixed ID and audit timestamp.

| Code | Name |
| --- | --- |
| HR | Human Resources |
| FIN | Finance |
| SALES | Sales |
| OPS | Operations |
| MGMT | Management |

## Designations

DesignationMaster inserts six active LCAP designations through Infrastructure/Persistence/Seeds/DesignationSeedData.cs. All have IsManagerial=false, following the requested default; Grade, Level, and Description remain null. Each has a fixed ID and seed timestamp.

| Code | Name |
| --- | --- |
| EXEC | Executive |
| SREXEC | Senior Executive |
| TL | Team Leader |
| MGR | Manager |
| HRM | HR Manager |
| PAYADMIN | Payroll Admin |

## Patna work location

WorkLocationMaster inserts PATNA-OFFICE (Patna Office) under LCAP / PATNA-HO. City is Patna, State is Bihar, Country is India. Latitude and Longitude are both null until actual coordinates are supplied; no coordinates were fabricated. AllowedRadiusMeters is 100, IsGeoFenceEnabled is true, and IsActive is true. Address and PinCode remain null. The enabled flag is configuration only and does not make a null-coordinate location a usable geofence.

ShiftMaster adds LCAP GENERAL / General Shift, 09:30-18:30, grace 15 minutes, IsNightShift=false and IsActive=true. MinimumHalfDayMinutes and MinimumFullDayMinutes remain null until specified.
