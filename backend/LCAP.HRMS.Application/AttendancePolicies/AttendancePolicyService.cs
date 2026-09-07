using System.ComponentModel.DataAnnotations;
using LCAP.HRMS.Application.Abstractions.Persistence;
using LCAP.HRMS.Application.Common.Exceptions;
using LCAP.HRMS.Application.Employees;
using LCAP.HRMS.Domain.AttendancePolicies;
using Microsoft.Extensions.Logging;
namespace LCAP.HRMS.Application.AttendancePolicies;

public sealed class AttendancePolicyService(IAttendancePolicyRepository repository, IUnitOfWork work, IEmployeeAccess access, ILogger<AttendancePolicyService> logger) : IAttendancePolicyService
{
    private void Scope(Guid company) { if (!access.CanWrite || (!access.IsSuperAdmin && (access.CompanyId is null || access.CompanyId != company))) throw new ForbiddenException(); }
    public Task<IReadOnlyList<AttendancePolicy>> ListAsync(PolicyPage q, CancellationToken ct)
    {
        if (!access.CanWrite) throw new ForbiddenException(); if (q.Skip < 0 || q.Take is < 1 or > 1000) throw new ValidationException("Invalid page.");
        if (!access.IsSuperAdmin) { Scope(access.CompanyId ?? Guid.Empty); if (q.CompanyId is { } c) Scope(c); q = q with { CompanyId = access.CompanyId }; }
        return repository.ListAsync(q, ct);
    }
    public async Task<AttendancePolicy> GetAsync(Guid id, CancellationToken ct) { var p = await repository.GetAsync(id, ct) ?? throw new NotFoundException("Policy was not found."); Scope(p.CompanyId); return p; }
    public async Task<AttendancePolicy?> CurrentAsync(Guid companyId, DateOnly date, CancellationToken ct) { Scope(companyId); return await repository.EffectiveAsync(companyId, date, ct); }
    public Task<AttendancePolicy> SaveAsync(AttendancePolicyRequest r, Guid? id, CancellationToken ct)
    {
        Scope(r.CompanyId); Validator.ValidateObject(r, new ValidationContext(r), true);
        return repository.WriteAsync(r.CompanyId, async () =>
        {
            if (!await repository.CompanyExistsAsync(r.CompanyId, ct)) throw new ValidationException("Company is unavailable.");
            var p = id is { } key ? await GetAsync(key, ct) : new AttendancePolicy { CompanyId = r.CompanyId };
            if (p.CompanyId != r.CompanyId) throw new ValidationException("Policy company cannot change.");
            r.PolicyCode = r.PolicyCode.Trim().ToUpperInvariant(); r.PolicyName = r.PolicyName.Trim(); r.Description = string.IsNullOrWhiteSpace(r.Description) ? null : r.Description.Trim();
            if (await repository.CodeExistsAsync(r.CompanyId, r.PolicyCode, id, ct)) throw new ConflictException("PolicyCode is already in use within this company.");
            p.PolicyCode = r.PolicyCode;
            p.PolicyName = r.PolicyName;
            p.Description = r.Description;
            p.GracePeriodMinutes = r.GracePeriodMinutes;
            p.LateRuleEnabled = r.LateRuleEnabled;
            p.ConsecutiveLateThreshold = r.ConsecutiveLateThreshold;
            p.PenaltyTriggerMode = r.PenaltyTriggerMode;
            p.PenaltyType = r.PenaltyType;
            p.PenaltyValue = r.PenaltyValue;
            p.ResetMode = r.ResetMode;
            p.EffectiveFrom = r.EffectiveFrom!.Value;
            p.EffectiveTo = r.EffectiveTo;
            p.IsDefault = r.IsDefault;
            p.IsActive = r.IsActive;
            if (await repository.OverlapAsync(p, ct)) throw new ConflictException("An active default policy already covers this effective period.");
            if (id is null) await repository.AddAsync(p, ct); else p.Revision++;
            await work.SaveChangesAsync(ct); logger.LogInformation("Attendance policy {Action}. Company {CompanyId}; policy {PolicyId}; revision {Revision}; active {Active}", id is null ? "created" : "updated", p.CompanyId, p.Id, p.Revision, p.IsActive); return p;
        }, ct);
    }
    public async Task DeleteAsync(Guid id, CancellationToken ct)
    {
        var p = await GetAsync(id, ct); await repository.WriteAsync(p.CompanyId, async () =>
        {
            p = await GetAsync(id, ct); p.IsDeleted = true; p.IsActive = false; p.Revision++; await work.SaveChangesAsync(ct); logger.LogInformation("Attendance policy deleted. Company {CompanyId}; policy {PolicyId}", p.CompanyId, p.Id); return true;
        }, ct);
    }
}
public sealed class CompanyDefaultAttendancePolicyResolver(IAttendancePolicyRepository repository) : IAttendancePolicyResolver
{
    public Task<AttendancePolicy?> ResolveAsync(Guid employeeId, Guid companyId, DateOnly attendanceDate, CancellationToken ct) => repository.EffectiveAsync(companyId, attendanceDate, ct);
}

