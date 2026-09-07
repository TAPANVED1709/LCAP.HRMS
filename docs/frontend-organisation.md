# Organisation frontend verification

Implemented on 2026-09-07.

## Features

Navy primary navigation, white topbar, light workspace, compact summary cards, six routed organisation tabs, shared searchable table, reactive form controls, status changes and confirmed soft-delete.

The UI integrates with the existing REST routes and response envelope. Bearer authentication is preserved; session-only token entry bridges the existing unconfigured sign-in flow.

## Automated checks

- 11 tests passed: six frontend field sets match the actual ASP.NET writable DTOs; optional tax fields survive updates; audit fields are excluded; midnight times, nullable thresholds, overnight shifts, coordinate pairing/precision and explicit false values are preserved.
- Angular production build passed with no compiler errors or budget warnings.
- Production output includes IIS SPA web.config.

## Browser checks

Using the separate local fixture server and disposable records:

- Disconnected session shows a 401 error and a route to API connection.
- Token connection loads the six master datasets.
- Company required validation, create, search, edit, deactivate, inactive filter, delete cancellation and confirmed delete worked.
- Branch editor loaded the owning company, address and head-office flag.
- Department editor disabled company reassignment and excluded the record itself from parent options.
- Designation editor displayed nullable grade/level and managerial flag.
- Equal shift times were rejected; 22:00–06:00 saved and returned with the night-shift flag.
- Work-location coordinates remained blank; supplying only one coordinate was rejected.
- Desktop and 390 × 844 mobile layouts were visually inspected; mobile sidebar and horizontal table scrolling worked.
- No JavaScript errors were captured during these checks.

## Limits

Browser CRUD checks used a local HTTP fixture, not a live SQL Server-backed API. Live identity-provider sign-in, SQL Server and IIS deployment need environment configuration. The fixture does not replace backend integration tests and is excluded from deployed assets. No production database was modified.

Record search/paging runs client-side after loading master records. Backend concurrency controls and interactive OIDC sign-in are outside this Day 1 implementation.

## Day 1 integration follow-up

The subsequent [Day 1 integration report](DAY-01-REPORT.md) verifies the production frontend against the real ASP.NET API and an isolated SQL Server 2022 database. Its evidence supersedes the earlier fixture-only connectivity limitation above.
