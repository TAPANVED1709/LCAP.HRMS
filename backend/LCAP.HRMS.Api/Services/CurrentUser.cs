using System.Security.Claims;
using LCAP.HRMS.Application.Abstractions;

namespace LCAP.HRMS.Api.Services;

public sealed class CurrentUser(IHttpContextAccessor accessor) : ICurrentUser
{
    public string? UserId => accessor.HttpContext?.User is { Identity.IsAuthenticated: true } user
        ? user.FindFirstValue("sub") ?? user.FindFirstValue(ClaimTypes.NameIdentifier)
        : null;
}
