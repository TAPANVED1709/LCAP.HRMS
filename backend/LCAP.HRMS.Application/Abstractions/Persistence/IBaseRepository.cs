using System.Linq.Expressions;
using LCAP.HRMS.Domain.Common;

namespace LCAP.HRMS.Application.Abstractions.Persistence;

public interface IBaseRepository<T> where T : BaseEntity
{
    // Returns a tracked entity for editing. Soft-deleted rows are excluded.
    Task<T?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    // Read-only, bounded page. Modify entities loaded through GetByIdAsync instead.
    Task<IReadOnlyList<T>> ListAsync(int skip = 0, int take = 100,
        Expression<Func<T, bool>>? predicate = null, CancellationToken cancellationToken = default);
    Task AddAsync(T entity, CancellationToken cancellationToken = default);
    void Remove(T entity);
}
