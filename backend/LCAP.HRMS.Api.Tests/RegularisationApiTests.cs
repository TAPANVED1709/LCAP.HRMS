using System.Net;
using System.Net.Http.Json;
using LCAP.HRMS.Api.Contracts;
using LCAP.HRMS.Application.AttendancePolicies;
using LCAP.HRMS.Application.Regularisation;
using LCAP.HRMS.Domain.Regularisation;
using LCAP.HRMS.Domain.Employees;
using LCAP.HRMS.Domain.Enums;
using LCAP.HRMS.Domain.Attendance;
using LCAP.HRMS.Infrastructure.Persistence;
using LCAP.HRMS.Infrastructure.Persistence.Seeds;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace LCAP.HRMS.Api.Tests;
[Collection("API integration")]
public sealed class RegularisationApiTests
{
    private sealed class Clock : TimeProvider
    {
        public DateTimeOffset Now = new(2026, 9, 20, 13, 0, 0, TimeSpan.Zero);
        public override DateTimeOffset GetUtcNow() => Now;
    }

    private sealed class Fixture : IDisposable
    {
        public Clock Clock = new();
        public CompanyApiFactory Factory;
        public HttpClient Self, Manager, Hr;
        public Guid EmployeeId, ManagerId, OtherManagerId;
        public Fixture()
        {
            Factory = new(Clock);
            Hr = Factory.CreateEmployeeClient("HRAdmin", CompanySeedData.LcapId);
            using var scope = Factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            Employee Make(string code) => new()
            {
                CompanyId = CompanySeedData.LcapId,
                BranchId = db.Branches.First().Id,
                DepartmentId = db.Departments.First().Id,
                DesignationId = db.Designations.First().Id,
                ShiftId = db.Shifts.First().Id,
                WorkLocationId = db.WorkLocations.First().Id,
                EmployeeCode = code,
                FirstName = code,
                DateOfJoining = new(2026, 1, 1),
                MobileNumber = "0000000000",
                EmploymentType = EmploymentType.Permanent,
                EmployeeStatus = EmployeeStatus.Active
            };
            var manager = Make("TEST-MANAGER");
            var other = Make("TEST-OTHER-MANAGER");
            db.AddRange(manager, other);
            db.SaveChanges();
            var e = Make("TEST-REG");
            e.ReportingManagerId = manager.Id;
            db.Add(e);
            db.SaveChanges();
            EmployeeId = e.Id;
            ManagerId = manager.Id;
            OtherManagerId = other.Id;
            Self = Factory.CreateEmployeeClient("Employee", e.CompanyId, e.Id);
            Manager = Factory.CreateEmployeeClient("Manager", e.CompanyId, manager.Id);
        }

        public async Task<Guid> Raw(int day = 8, bool checkout = true, int minute = 50)
        {
            using var scope = Factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var e = db.Employees.Single(e => e.Id == EmployeeId);
            var r = new AttendanceRecord
            {
                CompanyId = e.CompanyId,
                EmployeeId = e.Id,
                ShiftId = e.ShiftId,
                WorkLocationId = e.WorkLocationId,
                AttendanceDate = new(2026, 9, day),
                TimeZoneId = "Asia/Kolkata",
                CheckInTime = new(2026, 9, day, 4, minute - 30, 0, TimeSpan.Zero),
                CheckOutTime = checkout ? new(2026, 9, day, 13, 0, 0, TimeSpan.Zero) : null,
                CheckInLatitude = 12.3456789m,
                CheckInLongitude = 13.7890123m,
                CheckInAccuracyMeters = 10,
                CheckInSource = "Synthetic",
                Status = checkout ? AttendanceStatus.Completed : AttendanceStatus.CheckedIn
            };
            db.Add(r);
            await db.SaveChangesAsync();
            await scope.ServiceProvider.GetRequiredService<IAttendanceEvaluationService>().EvaluateAsync(r.Id, default);
            return r.Id;
        }

        public RegularisationCreateRequest Body(RegularisationType type = RegularisationType.IncorrectCheckIn, int day = 8) => new()
        {
            AttendanceDate = new(2026, 9, day),
            RequestType = type,
            RequestedCheckInTime = type is RegularisationType.MissedCheckOut or RegularisationType.IncorrectCheckOut ? null : new(2026, 9, day, 4, 10, 0, TimeSpan.Zero),
            RequestedCheckOutTime = type is RegularisationType.MissedCheckOut or RegularisationType.IncorrectCheckOut or RegularisationType.IncorrectBoth or RegularisationType.AttendanceNotRecorded ? new(2026, 9, day, 13, 1, 0, TimeSpan.Zero) : null,
            EmployeeReason = "Biometric/mobile clocking issue"
        };
        public async Task<RegularisationResponse> Submit(RegularisationCreateRequest? body = null)
        {
            var response = await Self.PostAsJsonAsync("/api/attendance-regularisations", body ?? Body());
            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
            return (await response.Content.ReadFromJsonAsync<ApiResponse<RegularisationResponse>>())!.Data!;
        }

        public async Task<RegularisationResponse> Approve(Guid id)
        {
            var r = await Manager.PostAsJsonAsync($"/api/attendance-regularisations/{id}/approve", new { reviewerRemarks = "Verified" });
            Assert.Equal(HttpStatusCode.OK, r.StatusCode);
            return (await r.Content.ReadFromJsonAsync<ApiResponse<RegularisationResponse>>())!.Data!;
        }

        public Task<EffectiveAttendance?> Effective(int day = 8) => Read<EffectiveAttendance>(Self, $"/api/attendance/effective/me?date=2026-09-{day:00}");
        public void Dispose()
        {
            Self.Dispose();
            Manager.Dispose();
            Hr.Dispose();
            Factory.Dispose();
        }
    }

    private static async Task<T?> Read<T>(HttpClient client, string path) => (await client.GetFromJsonAsync<ApiResponse<T>>(path))!.Data;
    [Theory]
    [InlineData(RegularisationType.MissedCheckIn)]
    [InlineData(RegularisationType.MissedCheckOut)]
    [InlineData(RegularisationType.IncorrectCheckIn)]
    [InlineData(RegularisationType.IncorrectCheckOut)]
    [InlineData(RegularisationType.IncorrectBoth)]
    [InlineData(RegularisationType.AttendanceNotRecorded)]
    [InlineData(RegularisationType.Other)]
    public async Task Valid_types_submit_and_apply(RegularisationType type)
    {
        using var f = new Fixture();
        if (type is not (RegularisationType.MissedCheckIn or RegularisationType.AttendanceNotRecorded))
            await f.Raw(checkout: type != RegularisationType.MissedCheckOut);
        var body = f.Body(type);
        var r = await f.Submit(body);
        Assert.Equal(RegularisationStatus.PendingManager, r.Status);
        Assert.Equal(f.ManagerId, r.ReportingManagerId);
        Assert.Equal(RegularisationStatus.Applied, (await f.Approve(r.Id)).Status);
        var effective = await f.Effective();
        Assert.Equal(AttendanceSource.Regularised, effective!.Source);
        Assert.Equal(body.RequestedCheckInTime ?? r.OriginalCheckInTime, effective.EffectiveCheckInTime);
        Assert.Equal(body.RequestedCheckOutTime ?? r.OriginalCheckOutTime, effective.EffectiveCheckOutTime);
    }

    [Theory]
    [InlineData("future")]
    [InlineData("order")]
    [InlineData("reason")]
    [InlineData("requiredTime")]
    [InlineData("wrongField")]
    [InlineData("farDate")]
    [InlineData("type")]
    [InlineData("unsafeUrl")]
    public async Task Invalid_requests_rejected(string kind)
    {
        using var f = new Fixture();
        await f.Raw();
        var b = f.Body();
        switch (kind)
        {
            case "future":
                b.AttendanceDate = new(2027, 1, 1);
                break;
            case "order":
                b.RequestType = RegularisationType.IncorrectBoth;
                b.RequestedCheckOutTime = b.RequestedCheckInTime!.Value.AddHours(-1);
                break;
            case "reason":
                b.EmployeeReason = " ";
                break;
            case "requiredTime":
                b.RequestedCheckInTime = null;
                break;
            case "wrongField":
                b.RequestedCheckOutTime = b.RequestedCheckInTime;
                break;
            case "farDate":
                b.RequestedCheckInTime = b.RequestedCheckInTime!.Value.AddDays(-2);
                break;
            case "type":
                b.RequestType = (RegularisationType)99;
                break;
            case "unsafeUrl":
                b.AttachmentUrl = "javascript:alert(1)";
                break;
        }

        Assert.Equal(HttpStatusCode.BadRequest, (await f.Self.PostAsJsonAsync("/api/attendance-regularisations", b)).StatusCode);
    }

    [Theory]
    [InlineData("missing")]
    [InlineData("inactive")]
    [InlineData("deleted")]
    [InlineData("foreign")]
    public async Task Invalid_manager_rejected(string kind)
    {
        using var f = new Fixture();
        using (var scope = f.Factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var e = db.Employees.Single(e => e.Id == f.EmployeeId);
            var m = db.Employees.Single(e => e.Id == f.ManagerId);
            if (kind == "missing")
                e.ReportingManagerId = null;
            if (kind == "inactive")
                m.IsActive = false;
            if (kind == "deleted")
                m.IsDeleted = true;
            if (kind == "foreign")
            {
                var c = new LCAP.HRMS.Domain.Companies.Company
                {
                    CompanyCode = "TEST-FOREIGN",
                    CompanyName = "Synthetic",
                    PayrollDay = 7,
                    SalaryPaymentDay = 7
                };
                db.Add(c);
                db.SaveChanges();
                m.CompanyId = c.Id;
            }

            db.SaveChanges();
        }

        Assert.Equal(HttpStatusCode.Conflict, (await f.Self.PostAsJsonAsync("/api/attendance-regularisations", f.Body(RegularisationType.AttendanceNotRecorded))).StatusCode);
    }

    [Fact]
    public async Task Duplicate_active_and_overlapping_types_rejected()
    {
        using var f = new Fixture();
        await f.Raw();
        await f.Submit();
        Assert.Equal(HttpStatusCode.Conflict, (await f.Self.PostAsJsonAsync("/api/attendance-regularisations", f.Body())).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await f.Self.PostAsJsonAsync("/api/attendance-regularisations", f.Body(RegularisationType.IncorrectBoth))).StatusCode);
    }

    [Fact]
    public async Task Identity_spoofing_and_anonymous_rejected()
    {
        using var f = new Fixture();
        Assert.Equal(HttpStatusCode.BadRequest, (await f.Self.PostAsJsonAsync("/api/attendance-regularisations", new { employeeId = f.ManagerId, attendanceDate = "2026-09-08", requestType = 6, employeeReason = "Test" })).StatusCode);
        using var anonymous = f.Factory.CreateCompanyClient(false);
        Assert.Equal(HttpStatusCode.Unauthorized, (await anonymous.GetAsync("/api/attendance-regularisations/me")).StatusCode);
    }

    [Fact]
    public async Task Own_and_stored_manager_scope_are_enforced()
    {
        using var f = new Fixture();
        await f.Raw();
        var r = await f.Submit();
        Assert.Single((await Read<RegularisationResponse[]>(f.Self, "/api/attendance-regularisations/me"))!);
        using var other = f.Factory.CreateEmployeeClient("Employee", CompanySeedData.LcapId, f.OtherManagerId);
        Assert.Equal(HttpStatusCode.Forbidden, (await other.GetAsync($"/api/attendance-regularisations/me/{r.Id}")).StatusCode);
        Assert.Single((await Read<RegularisationResponse[]>(f.Manager, "/api/attendance-regularisations/my-team"))!);
        using var managerB = f.Factory.CreateEmployeeClient("Manager", CompanySeedData.LcapId, f.OtherManagerId);
        Assert.Empty((await Read<RegularisationResponse[]>(managerB, "/api/attendance-regularisations/my-team"))!);
        Assert.Equal(HttpStatusCode.Forbidden, (await managerB.PostAsJsonAsync($"/api/attendance-regularisations/{r.Id}/approve", new { })).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await f.Hr.PostAsJsonAsync($"/api/attendance-regularisations/{r.Id}/approve", new { })).StatusCode);
    }

    [Fact]
    public async Task Reject_requires_remarks_and_is_final()
    {
        using var f = new Fixture();
        await f.Raw();
        var r = await f.Submit();
        var path = $"/api/attendance-regularisations/{r.Id}";
        Assert.Equal(HttpStatusCode.BadRequest, (await f.Manager.PostAsJsonAsync(path + "/reject", new { })).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await f.Manager.PostAsJsonAsync(path + "/reject", new { reviewerRemarks = "Insufficient evidence" })).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await f.Manager.PostAsJsonAsync(path + "/approve", new { })).StatusCode);
        Assert.Equal(RegularisationStatus.Rejected, (await Read<RegularisationResponse>(f.Self, path))!.Status);
        await f.Submit();
    }

    [Fact]
    public async Task Cancel_only_pending_and_repeat_review_conflicts()
    {
        using var f = new Fixture();
        await f.Raw();
        var r = await f.Submit();
        var path = $"/api/attendance-regularisations/{r.Id}";
        Assert.Equal(HttpStatusCode.OK, (await f.Self.PostAsJsonAsync(path + "/cancel", new { cancellationReason = "Withdraw" })).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await f.Manager.PostAsJsonAsync(path + "/approve", new { })).StatusCode);
        var next = await f.Submit();
        await f.Approve(next.Id);
        Assert.Equal(HttpStatusCode.Conflict, (await f.Manager.PostAsJsonAsync($"/api/attendance-regularisations/{next.Id}/approve", new { })).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await f.Self.PostAsJsonAsync($"/api/attendance-regularisations/{next.Id}/cancel", new { cancellationReason = "Withdraw" })).StatusCode);
    }

    [Fact]
    public async Task Correction_preserves_raw_GPS_and_old_evaluation_and_flags_penalty()
    {
        using var f = new Fixture();
        for (int day = 8; day <= 11; day++)
            await f.Raw(day);
        var r = await f.Submit(f.Body(day: 11));
        var result = await f.Approve(r.Id);
        Assert.True(result.PolicyImpactReviewRequired);
        var state = await f.Effective(11);
        Assert.False(state!.Evaluation!.IsLate);
        Assert.Equal(AttendanceSource.Regularised, state.Source);
        Assert.Equal(2, state.Evaluation.EvaluationVersion);
        using var scope = f.Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var raw = await db.AttendanceRecords.SingleAsync(x => x.EmployeeId == f.EmployeeId && x.AttendanceDate == new DateOnly(2026, 9, 11));
        Assert.Equal(20, raw.CheckInTime!.Value.Minute);
        Assert.Equal(12.3456789m, raw.CheckInLatitude);
        Assert.Equal(13.7890123m, raw.CheckInLongitude);
        var original = await db.AttendanceEvaluations.SingleAsync(x => x.AttendanceRecordId == raw.Id);
        Assert.True(original.IsLate);
        Assert.True(original.PenaltyTriggered);
        Assert.Single(await db.AttendancePenaltyEvents.ToListAsync());
        var penalty = (await Read<PenaltyResponse[]>(f.Self, $"/api/employees/{f.EmployeeId}/attendance-penalties"))!.Single();
        Assert.True(penalty.PolicyImpactReviewRequired);
        Assert.Equal(LCAP.HRMS.Domain.AttendancePolicies.PenaltyEventStatus.Pending, penalty.Status);
        var audit = await Read<RegularisationAuditEntry[]>(f.Self, $"/api/attendance-regularisations/{r.Id}/audit");
        Assert.Contains(audit!, a => a.Action == "PolicyImpactReviewRequired");
    }

    [Fact]
    public async Task Missing_raw_does_not_fabricate_evidence()
    {
        using var f = new Fixture();
        var r = await f.Submit(f.Body(RegularisationType.AttendanceNotRecorded));
        await f.Approve(r.Id);
        var effective = await f.Effective();
        Assert.Null(effective!.AttendanceRecordId);
        Assert.Null(effective.OriginalCheckInTime);
        Assert.NotNull(effective.CorrectionId);
        using var scope = f.Factory.Services.CreateScope();
        Assert.Empty(await scope.ServiceProvider.GetRequiredService<ApplicationDbContext>().AttendanceRecords.ToListAsync());
    }

    [Fact]
    public async Task Missed_checkout_keeps_raw_null_but_allows_next_checkin()
    {
        using var f = new Fixture();
        await f.Raw(checkout: false);
        var r = await f.Submit(f.Body(RegularisationType.MissedCheckOut));
        await f.Approve(r.Id);
        using (var scope = f.Factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            Assert.Null((await db.AttendanceRecords.SingleAsync()).CheckOutTime);
            var location = await db.WorkLocations.FirstAsync();
            location.IsGeoFenceEnabled = false;
            await db.SaveChangesAsync();
        }

        var response = await f.Self.PostAsJsonAsync("/api/attendance/check-in", new { latitude = 0, longitude = 0, accuracyMeters = 10 });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task Manager_change_keeps_old_assignment()
    {
        using var f = new Fixture();
        await f.Raw();
        var r = await f.Submit();
        using (var scope = f.Factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            db.Employees.Single(e => e.Id == f.EmployeeId).ReportingManagerId = f.OtherManagerId;
            db.SaveChanges();
        }

        await f.Approve(r.Id);
        var next = await f.Submit(f.Body(RegularisationType.AttendanceNotRecorded, 9));
        Assert.Equal(f.OtherManagerId, next.ReportingManagerId);
        Assert.Equal(f.ManagerId, (await Read<RegularisationResponse>(f.Self, $"/api/attendance-regularisations/me/{r.Id}"))!.ReportingManagerId);
    }

    [Fact]
    public async Task Hr_register_is_company_scoped()
    {
        using var f = new Fixture();
        await f.Raw();
        var r = await f.Submit();
        Assert.Single((await Read<RegularisationResponse[]>(f.Hr, "/api/attendance-regularisations"))!);
        using var foreign = f.Factory.CreateEmployeeClient("HRAdmin", Guid.NewGuid());
        Assert.Empty((await Read<RegularisationResponse[]>(foreign, "/api/attendance-regularisations"))!);
        Assert.Equal(HttpStatusCode.Forbidden, (await foreign.GetAsync($"/api/attendance-regularisations/{r.Id}")).StatusCode);
    }

    [Fact]
    public async Task Overnight_correction_uses_next_calendar_day()
    {
        using var f = new Fixture();
        using (var scope = f.Factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var s = db.Shifts.First();
            s.StartTime = new(22, 0);
            s.EndTime = new(6, 0);
            s.IsNightShift = true;
            db.SaveChanges();
        }

        var b = f.Body(RegularisationType.AttendanceNotRecorded);
        b.RequestedCheckInTime = new(2026, 9, 8, 16, 30, 0, TimeSpan.Zero);
        b.RequestedCheckOutTime = new(2026, 9, 9, 0, 40, 0, TimeSpan.Zero);
        var r = await f.Submit(b);
        await f.Approve(r.Id);
        Assert.False((await f.Effective())!.Evaluation!.IsLate);
    }

    [Fact]
    public async Task History_is_immutable_and_duplicate_correction_is_prevented()
    {
        using var f = new Fixture();
        await f.Raw();
        var r = await f.Submit();
        await f.Approve(r.Id);
        using var scope = f.Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var c = await db.AttendanceCorrections.SingleAsync();
        db.Remove(c);
        await Assert.ThrowsAsync<LCAP.HRMS.Application.Common.Exceptions.ConflictException>(() => db.SaveChangesAsync());
        db.ChangeTracker.Clear();
        var request = await db.AttendanceRegularisationRequests.SingleAsync();
        request.IsDeleted = true;
        await Assert.ThrowsAsync<LCAP.HRMS.Application.Common.Exceptions.ConflictException>(() => db.SaveChangesAsync());
        db.ChangeTracker.Clear();
        db.Add(new AttendanceCorrection { CompanyId = c.CompanyId, EmployeeId = c.EmployeeId, RegularisationRequestId = c.RegularisationRequestId, AttendanceDate = c.AttendanceDate, Version = 2, Reason = "Duplicate", AppliedBy = "Test" });
        await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
    }

    [Fact]
    public async Task Changed_raw_evidence_requires_resubmission()
    {
        using var f = new Fixture();
        await f.Raw(checkout: false);
        var r = await f.Submit(f.Body(RegularisationType.MissedCheckOut));
        using (var scope = f.Factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var raw = await db.AttendanceRecords.SingleAsync();
            raw.CheckOutTime = new(2026, 9, 8, 13, 0, 0, TimeSpan.Zero);
            raw.Status = AttendanceStatus.Completed;
            await db.SaveChangesAsync();
        }

        Assert.Equal(HttpStatusCode.Conflict, (await f.Manager.PostAsJsonAsync($"/api/attendance-regularisations/{r.Id}/approve", new { })).StatusCode);
    }

    [Fact]
    public async Task Backdated_correction_revises_later_sequence_without_changing_originals()
    {
        using var f = new Fixture();
        for (int d = 8; d <= 12; d++)
            await f.Raw(d);
        var r = await f.Submit(f.Body(day: 8));
        await f.Approve(r.Id);
        Assert.False((await f.Effective(8))!.Evaluation!.IsLate);
        Assert.Equal(3, (await f.Effective(11))!.Evaluation!.ConsecutiveLateCount);
        Assert.True((await f.Effective(12))!.PolicyImpactReviewRequired);
        using var scope = f.Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        Assert.Equal(5, await db.AttendanceEvaluationRevisions.CountAsync());
        Assert.Equal(5, await db.AttendanceEvaluations.CountAsync());
        Assert.Single(await db.AttendancePenaltyEvents.ToListAsync());
    }

    [Fact]
    public async Task Regularised_only_date_cannot_receive_duplicate_raw_checkin()
    {
        using var f = new Fixture();
        f.Clock.Now = new(2026, 9, 20, 14, 0, 0, TimeSpan.Zero);
        var r = await f.Submit(f.Body(RegularisationType.AttendanceNotRecorded, 20));
        await f.Approve(r.Id);
        using (var scope = f.Factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            db.WorkLocations.First().IsGeoFenceEnabled = false;
            db.SaveChanges();
        }

        Assert.Equal(HttpStatusCode.Conflict, (await f.Self.PostAsJsonAsync("/api/attendance/check-in", new { latitude = 0, longitude = 0, accuracyMeters = 10 })).StatusCode);
    }

    [Fact]
    public async Task Reason_only_exception_does_not_reset_late_sequence()
    {
        using var f = new Fixture();
        for (int day = 8; day <= 10; day++)
            await f.Raw(day);
        var body = f.Body(RegularisationType.Other, 11);
        body.RequestedCheckInTime = null;
        body.RequestedCheckOutTime = null;
        var r = await f.Submit(body);
        await f.Approve(r.Id);
        var effective = await f.Effective(11);
        Assert.False(effective!.Evaluation!.IsEvaluated);
        Assert.Equal(3, effective.Evaluation.SequenceAfterEvaluation);
        await f.Raw(12);
        using var scope = f.Factory.Services.CreateScope();
        Assert.Single(await scope.ServiceProvider.GetRequiredService<ApplicationDbContext>().AttendancePenaltyEvents.ToListAsync());
    }
}
