using System.Net;
using System.Net.Http.Json;
using LCAP.HRMS.Api.Contracts;
using LCAP.HRMS.Application.Attendance;
using LCAP.HRMS.Application.AttendancePolicies;
using LCAP.HRMS.Domain.AttendancePolicies;
using LCAP.HRMS.Domain.Employees;
using LCAP.HRMS.Domain.Enums;
using LCAP.HRMS.Domain.WorkLocations;
using LCAP.HRMS.Infrastructure.Persistence;
using LCAP.HRMS.Infrastructure.Persistence.Seeds;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
namespace LCAP.HRMS.Api.Tests;

[Collection("API integration")]
public sealed class AttendancePolicyApiTests
{
    private sealed class Clock : TimeProvider { public DateTimeOffset Now = new(2026, 9, 8, 4, 18, 0, TimeSpan.Zero); public override DateTimeOffset GetUtcNow() => Now; }
    private sealed class Fixture : IDisposable
    {
        public Clock Clock = new(); public CompanyApiFactory Factory; public HttpClient Admin; public HttpClient Self; public Guid EmployeeId;
        public Fixture() { Factory = new(Clock); Admin = Factory.CreateEmployeeClient("HRAdmin", CompanySeedData.LcapId); using var scope = Factory.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>(); var l = new WorkLocation { CompanyId = CompanySeedData.LcapId, BranchId = db.Branches.First().Id, LocationCode = "TEST-POLICY", LocationName = "Synthetic", Latitude = 0, Longitude = 0 }; db.Add(l); db.SaveChanges(); var e = new Employee { CompanyId = l.CompanyId, BranchId = l.BranchId, WorkLocationId = l.Id, ShiftId = db.Shifts.First().Id, DepartmentId = db.Departments.First().Id, DesignationId = db.Designations.First().Id, EmployeeCode = "TEST-LATE", FirstName = "Synthetic", MobileNumber = "0000000000", DateOfJoining = new(2026, 1, 1), EmploymentType = EmploymentType.Permanent, EmployeeStatus = EmployeeStatus.Active }; db.Add(e); db.SaveChanges(); EmployeeId = e.Id; Self = Factory.CreateEmployeeClient("Employee", l.CompanyId, e.Id); }
        public void Dispose() { Self.Dispose(); Admin.Dispose(); Factory.Dispose(); }
        public async Task<AttendanceResponse> CheckIn() { var r = await Self.PostAsJsonAsync("/api/attendance/check-in", new { latitude = 0, longitude = 0, accuracyMeters = 10 }); Assert.Equal(HttpStatusCode.Created, r.StatusCode); return (await r.Content.ReadFromJsonAsync<ApiResponse<AttendanceResponse>>())!.Data!; }
        public async Task Out() { Assert.Equal(HttpStatusCode.OK, (await Self.PostAsJsonAsync("/api/attendance/check-out", new { latitude = 0, longitude = 0, accuracyMeters = 10 })).StatusCode); }
    }
    private static AttendancePolicyRequest Request() => new() { CompanyId = CompanySeedData.LcapId, PolicyCode = "TEST-POLICY", PolicyName = "Test Policy", GracePeriodMinutes = 15, EffectiveFrom = new(2026, 9, 8), IsDefault = false };
    [Fact]
    public async Task Lcap_scenario_auto_evaluates_and_repeats_are_idempotent()
    {
        using var f = new Fixture(); int[] mins = [48, 49, 46, 50, 48]; Guid id = Guid.Empty; for (int i = 0; i < 5; i++) { f.Clock.Now = new(2026, 9, 8 + i, 4, mins[i] - 30, 0, TimeSpan.Zero); var r = await f.CheckIn(); id = r.AttendanceId; Assert.NotNull(r.Evaluation); Assert.Equal(i == 4 ? 1 : i + 1, r.Evaluation.ConsecutiveLateCount); Assert.Equal(i == 3, r.Evaluation.PenaltyTriggered); await f.Out(); using var scope = f.Factory.Services.CreateScope(); await scope.ServiceProvider.GetRequiredService<IAttendanceEvaluationService>().EvaluateAsync(id, default); }
        var penalties = (await f.Self.GetFromJsonAsync<ApiResponse<PenaltyResponse[]>>($"/api/employees/{f.EmployeeId}/attendance-penalties"))!.Data!; Assert.Single(penalties); Assert.Equal(PenaltyEventStatus.Pending, penalties[0].Status);
        using var verify = f.Factory.Services.CreateScope(); var db = verify.ServiceProvider.GetRequiredService<ApplicationDbContext>(); Assert.Equal(5, await db.AttendanceEvaluations.CountAsync()); Assert.Equal(1, await db.AttendancePenaltyEvents.CountAsync()); Assert.All(await db.AttendanceRecords.ToListAsync(), r => Assert.Equal(0, r.CheckInLatitude));
    }
    [Theory]
    [InlineData("inactive")]
    [InlineData("deleted")]
    [InlineData("notEffective")]
    public async Task Missing_effective_policy_is_safe(string condition) { using var f = new Fixture(); using (var scope = f.Factory.Services.CreateScope()) { var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>(); var p = db.AttendancePolicies.Single(); if (condition == "inactive") p.IsActive = false; if (condition == "deleted") p.IsDeleted = true; if (condition == "notEffective") p.EffectiveFrom = new(2027, 1, 1); db.SaveChanges(); } var r = await f.CheckIn(); Assert.False(r.Evaluation!.IsEvaluated); Assert.Equal("PolicyNotConfigured", r.Evaluation.ReasonCode); Assert.False(r.Evaluation.PenaltyTriggered); }
    [Fact]
    public async Task Policy_CRUD_validation_scope_and_immutable_history()
    {
        using var f = new Fixture(); var r = await f.CheckIn(); var old = r.Evaluation!; var policy = (await f.Admin.GetFromJsonAsync<ApiResponse<AttendancePolicy[]>>("/api/attendance-policies"))!.Data!.Single(); var update = Request(); update.PolicyCode = policy.PolicyCode; update.PolicyName = policy.PolicyName; update.GracePeriodMinutes = 60; update.IsDefault = true;
        Assert.Equal(HttpStatusCode.OK, (await f.Admin.PutAsJsonAsync("/api/attendance-policies/" + policy.Id, update)).StatusCode);
        var after = (await f.Self.GetFromJsonAsync<ApiResponse<EvaluationResponse>>($"/api/attendance/{r.AttendanceId}/evaluation"))!.Data!; Assert.Equal(old.PolicyRevision, after.PolicyRevision); Assert.Equal(15, after.GracePeriodMinutes); Assert.True(after.IsLate);
        var summary = (await f.Self.GetFromJsonAsync<ApiResponse<LateSummary>>($"/api/employees/{f.EmployeeId}/late-summary"))!.Data!;
        Assert.Equal(0, summary.CurrentLateSequence); Assert.False(summary.NextLateTriggersPenalty);
        Assert.Equal(HttpStatusCode.NoContent, (await f.Admin.DeleteAsync("/api/attendance-policies/" + policy.Id)).StatusCode); Assert.Equal(HttpStatusCode.NotFound, (await f.Admin.GetAsync("/api/attendance-policies/" + policy.Id)).StatusCode);
        Assert.Equal(old.AttendancePolicyId, (await f.Self.GetFromJsonAsync<ApiResponse<EvaluationResponse>>($"/api/attendance/{r.AttendanceId}/evaluation"))!.Data!.AttendancePolicyId);
        var create = Request(); Assert.Equal(HttpStatusCode.Created, (await f.Admin.PostAsJsonAsync("/api/attendance-policies", create)).StatusCode); Assert.Equal(HttpStatusCode.Conflict, (await f.Admin.PostAsJsonAsync("/api/attendance-policies", create)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await f.Self.PostAsJsonAsync("/api/attendance-policies", create)).StatusCode);
        using var foreign = f.Factory.CreateEmployeeClient("HRAdmin", Guid.NewGuid()); Assert.Empty((await foreign.GetFromJsonAsync<ApiResponse<AttendancePolicy[]>>("/api/attendance-policies"))!.Data!); Assert.Equal(HttpStatusCode.Forbidden, (await foreign.PostAsJsonAsync("/api/attendance-policies", create)).StatusCode);
        using var other = f.Factory.CreateEmployeeClient("Employee", CompanySeedData.LcapId, Guid.NewGuid()); Assert.Equal(HttpStatusCode.Forbidden, (await other.GetAsync($"/api/employees/{f.EmployeeId}/late-summary")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await f.Self.GetAsync($"/api/employees/{f.EmployeeId}/late-summary")).StatusCode);
        var json = await f.Self.GetStringAsync($"/api/attendance/{r.AttendanceId}/evaluation"); Assert.DoesNotContain("latitude", json, StringComparison.OrdinalIgnoreCase); Assert.DoesNotContain("longitude", json, StringComparison.OrdinalIgnoreCase);
    }
    [Theory]
    [InlineData("grace")]
    [InlineData("threshold")]
    [InlineData("dates")]
    [InlineData("code")]
    [InlineData("enum")]
    [InlineData("dayValue")]
    public async Task Invalid_policy_rejected(string field) { using var f = new Fixture(); var p = Request(); switch (field) { case "grace": p.GracePeriodMinutes = -1; break; case "threshold": p.ConsecutiveLateThreshold = 0; break; case "dates": p.EffectiveTo = new(2026, 1, 1); break; case "code": p.PolicyCode = " "; break; case "enum": p.ResetMode = (ResetMode)99; break; case "dayValue": p.PenaltyValue = 100; break; } Assert.Equal(HttpStatusCode.BadRequest, (await f.Admin.PostAsJsonAsync("/api/attendance-policies", p)).StatusCode); }
    [Fact] public async Task Overlapping_defaults_rejected_and_effective_selection_inclusive() { using var f = new Fixture(); var p = Request(); p.IsDefault = true; Assert.Equal(HttpStatusCode.Conflict, (await f.Admin.PostAsJsonAsync("/api/attendance-policies", p)).StatusCode); var current = await f.Admin.GetFromJsonAsync<ApiResponse<AttendancePolicy>>($"/api/attendance-policies/current?companyId={p.CompanyId}&date=2026-09-08"); Assert.Equal("LCAP-STANDARD", current!.Data!.PolicyCode); var absent = await f.Admin.GetFromJsonAsync<ApiResponse<AttendancePolicy>>($"/api/attendance-policies/current?companyId={p.CompanyId}&date=2026-09-07"); Assert.Null(absent!.Data); }
    [Fact] public async Task No_attendance_means_no_evaluation_and_history_cannot_be_modified() { using var f = new Fixture(); using var scope = f.Factory.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>(); Assert.Empty(await db.AttendanceEvaluations.ToListAsync()); await f.CheckIn(); var e = await db.AttendanceEvaluations.SingleAsync(); e.LateMinutes = 0; await Assert.ThrowsAsync<LCAP.HRMS.Application.Common.Exceptions.ConflictException>(() => db.SaveChangesAsync()); }

    [Fact]
    public async Task Overnight_summary_uses_shift_start_date_at_policy_end_boundary()
    {
        using var f = new Fixture();
        using (var scope = f.Factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var shift = db.Shifts.First(); shift.StartTime = new(22, 0); shift.EndTime = new(6, 0); shift.IsNightShift = true;
            db.AttendancePolicies.Single().EffectiveTo = new(2026, 9, 8);
            db.SaveChanges();
        }
        f.Clock.Now = new(2026, 9, 8, 16, 48, 0, TimeSpan.Zero);
        var record = await f.CheckIn(); Assert.Equal(new DateOnly(2026, 9, 8), record.AttendanceDate);
        f.Clock.Now = new(2026, 9, 8, 19, 0, 0, TimeSpan.Zero);
        var summary = (await f.Self.GetFromJsonAsync<ApiResponse<LateSummary>>($"/api/employees/{f.EmployeeId}/late-summary"))!.Data!;
        Assert.Equal(1, summary.CurrentLateSequence); Assert.Equal(3, summary.Threshold);
    }
}
