# LCAP HRMS - Day 3 Handoff

## Target

Geo-fenced Attendance: check-in, check-out, GPS validation, attendance record creation, distance calculation, accuracy validation, and Employee-to-WorkLocation enforcement. None of this logic is implemented in Day 2.

## Existing foundation

- Employee belongs to Company, Branch, Department, Designation, Shift and Work Location; all six assignments are required.
- ReportingManagerId is optional and same-company. The service rejects cycles; SQL employee writes use a transactional application lock to serialize hierarchy edits across instances.
- Soft-delete, audit fields, bounded directory queries, HR-only employee writes and company/employee claim scoping exist.
- Work Location has nullable decimal coordinates, positive allowed radius (default 100 metres), and a geofence-enabled flag. Patna coordinates remain NULL.
- Shift supports overnight local times. Half/full-day duration thresholds remain unconfigured in the GENERAL seed.
- Day 2 added `20260907131530_EmployeeMaster`. Use the current generated deployment script and review migrations before application; startup does not migrate databases.

## Decisions required before attendance implementation

- Obtain actual office coordinates and confirm radius. Never substitute invented GPS data or treat missing coordinates as (0,0).
- Define timezone (including date assignment for overnight shifts), allowed check-in/out windows, and missed-checkout behaviour.
- Define acceptable GPS accuracy, stale-fix threshold, device timestamp handling, and whether offsite/remote exceptions are allowed.
- Decide multiple check-ins, breaks, retries/idempotency, overlapping shifts and manager correction rules.
- Define active employment eligibility and notice/suspension/exit behaviour. IsActive and EmployeeStatus are currently separate fields.
- Configure the production identity provider to issue verified `role`, `company_id` and `employee_id` claims. Employee self-service must use the authenticated employee mapping, not a caller-supplied employee ID alone.

## Implementation guidance

Reuse the existing layers and audit conventions. Validate current Employee assignments server-side on every check-in/out; do not trust frontend Company/Branch/WorkLocation values. Deny missing/deleted employees and unusable locations. Enforce company ownership before returning records.

Store server receipt time in UTC, and distinguish device event time from server time. Use an explicit timezone to determine the applicable shift/work date. Calculate distance with a tested geographic formula using consistent units and boundary handling. Reject or clearly flag absent, out-of-range, stale or inaccurate coordinates according to the agreed rules.

Keep statutory and bank identifiers out of attendance DTOs, logs and exports. Preserve sensitive-data protections and role/company scoping. Day 1 master write endpoints still use their original authentication-only policy; review those policies before production exposure. SQL foreign keys restrict hard deletes; company/branch ownership consistency is also checked at service level.

## Acceptance tests

- Valid check-in/out, repeated request idempotence, checkout without check-in and duplicate open records.
- Radius centre, exact boundary, outside radius, zero coordinates, missing office coordinates, invalid latitude/longitude and poor/stale accuracy.
- Employee assigned to another company/branch/location; inactive/deleted/unauthenticated employee; forged employee ID.
- Overnight shifts across midnight, timezone date boundary, missing checkout and concurrency.
- Scoped employee/manager/HR reads and no sensitive identifier leakage.
- Backend/Angular builds and tests, real SQL Server migration repeatability, and browser check-in/out flow.

## Production considerations

IIS, HTTPS bindings, external OIDC discovery/rotation, SQL certificate configuration and database encryption/access policy need deployment validation. Day 2 uses a test-only loopback host and synthetic employees. Do not deploy the harness or its token issuer. Review encryption at rest/backup protection, optimistic concurrency and authorization of existing Organisation masters before rollout.
