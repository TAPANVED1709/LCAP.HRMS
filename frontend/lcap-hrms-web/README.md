# LCAP HRMS admin frontend

Angular 21 standalone application for Settings → Organisation.

## Run locally

From this folder:

```powershell
npm ci
npm start
```

Open http://localhost:4200/settings/organisation/companies.
The development proxy forwards /api/** to http://localhost:5080.

Start the ASP.NET API using the root README instructions. Its SQL Server database must have the reviewed master migrations applied and its JWT authority/audience configured. The frontend never creates a database or applies migrations.

The API protects every master endpoint. Open Profile → API connection and supply a valid access token from the configured identity provider. The token is kept only in memory for the current tab, sent only to relative /api/ requests, and cleared on reload or Disconnect. The Administrator label is a profile placeholder; it does not grant permissions. Interactive identity-provider sign-in and token refresh are not implemented.

## Organisation screens

- Companies: legal details, statutory identifiers, address, payroll preferences.
- Branches: company, address/contact details, head-office flag.
- Departments: company and parent hierarchy; self/descendant parents excluded, company locked during editing.
- Designations: job title, description, grade, nullable level, managerial flag.
- Shifts: local time-only values, overnight support, grace and nullable attendance thresholds.
- Work locations: matching company/branch, address, nullable coordinate pair, radius and geofence flag.

Each screen supports Add, Edit, Activate/Deactivate, confirmed soft-delete, case-insensitive search, status filtering, and ten-row client-side pagination. API lists are fetched in batches of 1,000 until complete so search includes all returned master records. Server-side search and total-count paging are future scalability work.

Writes send the complete writable DTO; identity/audit/deletion fields are excluded. Status changes first fetch the latest record, preserving fields not shown in the table. The existing backend does not expose optimistic concurrency tokens, so concurrent full-record updates still follow its last-write behaviour.

Validation matches required fields, lengths and ranges, plus email/URL, integer values, coordinate precision/pairing, and shift threshold ordering. Duplicate-code and backend validation errors remain visible in the editor. Loading, retryable errors, success messages, accessible labels, native modal focus trapping, and responsive navigation are included.

Dashboard, Employees, Attendance, Leave, Payroll, Payslips and Reports are disabled navigation placeholders. No attendance or payroll screens are implemented.

## Structure

```text
src/app/
  app.*                         Sidebar, topbar, profile and route shell
  core/api.service.ts           HTTP API, bearer interceptor and session
  organisation/
    master-config.ts            Six master definitions and form metadata
    master-utils.ts             Writable payloads and cross-field validation
    organisation.ts/.html       Routed table/editor orchestration
  shared/
    data-table.ts               Search, status filter, paging and row actions
    form-control.ts             Shared reactive inputs and validation messages
    icon.ts                     Local SVG icon component
src/styles.scss                 Enterprise theme and responsive layout
public/web.config               IIS SPA fallback; excludes /api
scripts/contracts.test.mjs      Automated DTO/payload tests
scripts/fixture-server.mjs      Isolated browser verification only
```

## Build and test

```powershell
npm test
npm run build
```

Production output: dist/lcap-hrms-web/browser.
Tests compare all six frontend field sets with the actual ASP.NET request DTOs and exercise payload preservation, nulls, false switches, midnight/overnight times, thresholds and coordinate validation.

For repeatable visual/interaction checks without SQL Server:

```powershell
npm run build
npm run preview:fixture
```

Open http://127.0.0.1:4300, then enter **fixture-token** in Profile → API connection. This separate test server uses disposable in-memory records. It is not a complete backend emulator, is not imported by the application, and is not part of the production output. Stop it with Ctrl+C. Never configure the real API to accept this test token.

## IIS

Deploy the contents of dist/lcap-hrms-web/browser to the static site. web.config is included automatically. Install IIS URL Rewrite and configure an HTTPS same-origin /api reverse proxy to the ASP.NET API site; retain the /api path. The Angular development proxy is not deployed. API requests must not fall through to index.html.

See [IIS deployment](../../deployment/README.md) and [verification](../../docs/frontend-organisation.md).
