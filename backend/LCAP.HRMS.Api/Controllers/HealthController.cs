using LCAP.HRMS.Api.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LCAP.HRMS.Api.Controllers;

[ApiController]
[Route("api/health")]
public sealed class HealthController : ControllerBase
{
    // Liveness only: no database or external identity provider is required.
    [AllowAnonymous]
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<HealthStatus>), StatusCodes.Status200OK)]
    public ActionResult<ApiResponse<HealthStatus>> Get() => Ok(ApiResponse<HealthStatus>.Ok(
        new HealthStatus("Healthy", DateTimeOffset.UtcNow), HttpContext.TraceIdentifier));
}

public sealed record HealthStatus(string Status, DateTimeOffset TimestampUtc);
