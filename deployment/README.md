# IIS deployment

This scaffold targets Windows Server with IIS. These are deployment instructions, not an executed deployment.

## API

1. Install IIS and the supported .NET 8 Hosting Bundle on the server. If IIS is installed after the bundle, repair the bundle. Restart IIS as required.
2. Publish from the repository root:

   ```powershell
   dotnet publish backend/LCAP.HRMS.Api/LCAP.HRMS.Api.csproj --configuration Release --output artifacts/api
   ```

3. Create a dedicated IIS application pool using **No Managed Code**, with 64-bit execution. Deploy the contents of artifacts/api to the API site's physical directory. The Web SDK generates the API web.config during publish.
4. Set Production configuration: ASPNETCORE_ENVIRONMENT=Production, ConnectionStrings__DefaultConnection, Jwt__Authority, Jwt__Audience, AllowedHosts, and any Cors__AllowedOrigins__0 entries. Set AllowedHosts to the API hostname (no scheme or port). Avoid storing secrets in source control.
5. Configure an HTTPS binding with a trusted certificate. Bind HTTP only if using HTTPS redirection. Use IIS-managed hosting; do not expose Kestrel separately.
6. Grant the application pool identity read/execute access to the published folder and only the SQL permissions needed at runtime. Windows authentication to remote SQL Server needs a suitable domain service identity, such as a configured gMSA. Use a separate deployment identity for schema changes.
7. Collect application logs using your server logging setup. Enable ASP.NET Core stdout logs only temporarily for startup troubleshooting; provision a writable logs directory if doing so.
8. Verify GET /api/health over HTTPS. Swagger is intentionally disabled in Production. Liveness does not verify database connectivity.

## Angular

```powershell
cd frontend/lcap-hrms-web
npm ci
npm run build
```

Deploy dist/lcap-hrms-web/browser to a separate IIS static site with HTTPS. Install IIS URL Rewrite. The Angular build includes public/web.config at the output root to support client-side routes.

Organisation screens use relative /api URLs. Configure a same-origin /api reverse proxy using IIS ARR/URL Rewrite to the API site, preserving the /api path. Configure the API identity provider and supply valid bearer tokens through the current session-only connection form; interactive sign-in is not yet implemented. The supplied static-site rewrite deliberately excludes /api so API requests never return the SPA's index.html.

Do not copy frontend.web.config into the API publish directory; the API uses the Web SDK-generated web.config.

## Database

Provision SQL Server 2022 with a trusted TLS certificate. InitialFoundation is the baseline; CompanyMaster, BranchMaster, DepartmentMaster, DesignationMaster, WorkLocationMaster, and ShiftMaster add their respective tables and seeds. Review database/scripts/ShiftMaster.sql and apply it as a separate deployment step. The API never runs migrations automatically. Generating a migration or SQL script does not apply it to a database. PAN, TAN, GSTIN, and unspecified seed contact details remain null.
