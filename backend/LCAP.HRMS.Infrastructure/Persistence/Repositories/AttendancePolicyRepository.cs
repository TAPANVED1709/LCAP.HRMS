using System.Data;
using LCAP.HRMS.Application.AttendancePolicies;
using LCAP.HRMS.Domain.AttendancePolicies;
using Microsoft.EntityFrameworkCore;
namespace LCAP.HRMS.Infrastructure.Persistence.Repositories;

public sealed class AttendancePolicyRepository(ApplicationDbContext context) : IAttendancePolicyRepository
{
    private IQueryable<AttendancePolicy> Policies => context.AttendancePolicies.Where(p => context.Companies.Any(c => c.Id == p.CompanyId));
    public Task<AttendancePolicy?> GetAsync(Guid id, CancellationToken ct) => Policies.SingleOrDefaultAsync(p => p.Id == id, ct);
    public async Task<IReadOnlyList<AttendancePolicy>> ListAsync(PolicyPage q, CancellationToken ct) => await Policies.AsNoTracking().Where(p => q.CompanyId == null || p.CompanyId == q.CompanyId).OrderBy(p => p.PolicyCode).ThenBy(p => p.Id).Skip(q.Skip).Take(q.Take).ToListAsync(ct);
    public Task<AttendancePolicy?> EffectiveAsync(Guid companyId, DateOnly date, CancellationToken ct) => Policies.AsNoTracking().Where(p => p.CompanyId == companyId && p.IsActive && p.IsDefault && p.EffectiveFrom <= date && (p.EffectiveTo == null || p.EffectiveTo >= date)).SingleOrDefaultAsync(ct);
    public Task<bool> CompanyExistsAsync(Guid id, CancellationToken ct) => context.Companies.AnyAsync(c => c.Id == id && c.IsActive, ct);
    public Task<bool> CodeExistsAsync(Guid companyId, string code, Guid? except, CancellationToken ct) => context.AttendancePolicies.IgnoreQueryFilters().AnyAsync(p => p.CompanyId == companyId && p.PolicyCode == code && (except == null || p.Id != except), ct);
    public Task<bool> OverlapAsync(AttendancePolicy p, CancellationToken ct) => p.IsDefault && p.IsActive && !p.IsDeleted ? context.AttendancePolicies.AnyAsync(x => x.Id != p.Id && x.CompanyId == p.CompanyId && x.IsDefault && x.IsActive && (p.EffectiveTo == null || x.EffectiveFrom <= p.EffectiveTo) && (x.EffectiveTo == null || x.EffectiveTo >= p.EffectiveFrom), ct) : Task.FromResult(false);
    public async Task AddAsync(AttendancePolicy p, CancellationToken ct) => await context.AddAsync(p, ct);
    public Task<T> WriteAsync<T>(Guid companyId, Func<Task<T>> action, CancellationToken ct) => context.Database.CreateExecutionStrategy().ExecuteAsync(async () =>
    {
        context.ChangeTracker.Clear(); await using var tx = await context.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        if (context.Database.IsSqlServer()) { var resource = "LCAP.AttendancePolicy." + companyId; await context.Database.ExecuteSqlInterpolatedAsync($"DECLARE @r int; EXEC @r=sys.sp_getapplock @Resource={resource},@LockMode='Exclusive',@LockOwner='Transaction',@LockTimeout=10000; IF @r<0 THROW 51000,'Policy is busy. Retry the request.',1;", ct); }
        var result = await action(); await tx.CommitAsync(ct); return result;
    });
}


