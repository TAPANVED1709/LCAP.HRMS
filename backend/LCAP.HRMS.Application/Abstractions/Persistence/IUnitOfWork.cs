namespace LCAP.HRMS.Application.Abstractions.Persistence;

// Implemented directly by the scoped DbContext; no separate transaction wrapper.
public interface IUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
