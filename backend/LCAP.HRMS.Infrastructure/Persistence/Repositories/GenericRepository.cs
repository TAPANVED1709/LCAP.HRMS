using System.Linq.Expressions;
using LCAP.HRMS.Application.Abstractions.Persistence;
using LCAP.HRMS.Domain.Common;
using Microsoft.EntityFrameworkCore;

namespace LCAP.HRMS.Infrastructure.Persistence.Repositories;

public class GenericRepository<T>(ApplicationDbContext context) : IBaseRepository<T> where T : BaseEntity
{
    protected ApplicationDbContext Context { get; } = context;

    public async Task<T?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        // FindAsync can return tracked deleted entities without executing the global filter.
        var entity = await Context.Set<T>().SingleOrDefaultAsync(entity => entity.Id == id, cancellationToken);
        return entity is { IsDeleted: false } && Context.Entry(entity).State != EntityState.Deleted ? entity : null;
    }

    public async Task<IReadOnlyList<T>> ListAsync(int skip = 0, int take = 100,
        Expression<Func<T, bool>>? predicate = null, CancellationToken cancellationToken = default)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(skip);
        if (take is < 1 or > 1000)
            throw new ArgumentOutOfRangeException(nameof(take), "Page size must be between 1 and 1000.");

        var query = Context.Set<T>().AsNoTracking();
        if (predicate is not null)
            query = query.Where(predicate);
        return await query.OrderBy(entity => entity.Id).Skip(skip).Take(take).ToListAsync(cancellationToken);
    }

    public async Task AddAsync(T entity, CancellationToken cancellationToken = default) =>
        await Context.Set<T>().AddAsync(entity, cancellationToken);

    // Persistence is deferred until IUnitOfWork.SaveChangesAsync.
    public void Remove(T entity) => Context.Set<T>().Remove(entity);
}
