namespace LCAP.HRMS.Application.Abstractions;

public interface ICurrentUser
{
    // Stable subject identifier; null for anonymous requests or background execution.
    string? UserId { get; }
}
