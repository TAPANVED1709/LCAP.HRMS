using System.Net;
using System.Net.Http.Json;
using LCAP.HRMS.Api.Contracts;
using LCAP.HRMS.Application.Attendance;
using LCAP.HRMS.Domain.Employees;
using LCAP.HRMS.Domain.Enums;
using LCAP.HRMS.Domain.WorkLocations;
using LCAP.HRMS.Domain.Attendance;
using LCAP.HRMS.Infrastructure.Persistence;
using LCAP.HRMS.Infrastructure.Persistence.Seeds;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
namespace LCAP.HRMS.Api.Tests;

[Collection("API integration")]
public sealed class AttendanceApiTests
{
    private sealed class Clock : TimeProvider { public DateTimeOffset Now { get; set; } = new(2026, 9, 7, 4, 0, 0, TimeSpan.Zero); public override DateTimeOffset GetUtcNow() => Now; }
    private sealed class FixedDistance(double value) : IGeoDistanceService { public double DistanceMeters(double a, double b, double c, double d) => value; }
    private sealed class Fixture : IDisposable
    {
        public Clock Clock { get; } = new(); public CompanyApiFactory Factory { get; }
        public HttpClient Client { get; }
        public Guid EmployeeId { get; }
        public Guid LocationId { get; }
        public Fixture(IGeoDistanceService? distance = null)
        {
            Factory = new(Clock, distance); using var init = Factory.CreateEmployeeClient();
            using var scope = Factory.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var location = new WorkLocation { CompanyId = CompanySeedData.LcapId, BranchId = db.Branches.First().Id, LocationCode = "TEST-GEO", LocationName = "Synthetic Test Location", Latitude = 0, Longitude = 0, AllowedRadiusMeters = 100 };
            db.Add(location); db.SaveChanges(); LocationId = location.Id;
            var employee = new Employee
            {
                CompanyId = CompanySeedData.LcapId,
                BranchId = location.BranchId,
                DepartmentId = db.Departments.First().Id,
                DesignationId = db.Designations.First().Id,
                ShiftId = db.Shifts.First().Id,
                WorkLocationId = location.Id,
                EmployeeCode = "TEST-ATTENDANCE",
                FirstName = "Test",
                MobileNumber = "0000000000",
                DateOfJoining = new(2026, 1, 1),
                EmploymentType = EmploymentType.Permanent,
                EmployeeStatus = EmployeeStatus.Active
            };
            db.Add(employee); db.SaveChanges(); EmployeeId = employee.Id;
            Client = Factory.CreateEmployeeClient("Employee", CompanySeedData.LcapId, EmployeeId);
        }
        public void Change(Action<ApplicationDbContext> change) { using var scope = Factory.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>(); change(db); db.SaveChanges(); }
        public void Dispose() { Client.Dispose(); Factory.Dispose(); }
    }
    private static AttendanceCheckInRequest Position(decimal latitude = 0, decimal longitude = 0, decimal accuracy = 10) => new() { Latitude = latitude, Longitude = longitude, AccuracyMeters = accuracy };
    private static async Task<AttendanceResponse> In(Fixture f) { var response = await f.Client.PostAsJsonAsync("/api/attendance/check-in", Position()); Assert.Equal(HttpStatusCode.Created, response.StatusCode); return (await response.Content.ReadFromJsonAsync<ApiResponse<AttendanceResponse>>())!.Data!; }
    [Fact]
    public async Task Valid_check_in_out_today_history_and_no_sensitive_fields()
    {
        using var f = new Fixture(); var r = await In(f); Assert.Equal(f.Clock.Now, r.CheckInTime); Assert.Equal("Asia/Kolkata", r.TimeZoneId); Assert.Equal(AttendanceStatus.CheckedIn, r.Status); Assert.True(r.WithinGeofence); Assert.Equal(0, r.DistanceMeters);
        Assert.Equal(HttpStatusCode.Conflict, (await f.Client.PostAsJsonAsync("/api/attendance/check-in", Position())).StatusCode);
        f.Clock.Now = f.Clock.Now.AddHours(8);
        var response = await f.Client.PostAsJsonAsync("/api/attendance/check-out", Position()); Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var done = (await response.Content.ReadFromJsonAsync<ApiResponse<AttendanceResponse>>())!.Data!; Assert.Equal(r.AttendanceId, done.AttendanceId); Assert.Equal(AttendanceStatus.Completed, done.Status); Assert.Equal(f.Clock.Now, done.CheckOutTime);
        Assert.Equal(HttpStatusCode.Conflict, (await f.Client.PostAsJsonAsync("/api/attendance/check-out", Position())).StatusCode);
        var today = (await f.Client.GetFromJsonAsync<ApiResponse<AttendanceTodayResponse>>("/api/attendance/me/today"))!.Data!; Assert.Equal(done.AttendanceId, today.Record!.AttendanceId);
        var history = await f.Client.GetStringAsync("/api/attendance/me"); foreach (var key in new[] { "latitude", "longitude", "pan", "aadhaarNumber", "bankAccountNumber", "mobileNumber" }) Assert.DoesNotContain('"' + key + '"', history, StringComparison.OrdinalIgnoreCase);
    }
    [Theory]
    [InlineData("latitude")]
    [InlineData("longitude")]
    [InlineData("missing")]
    [InlineData("accuracy")]
    [InlineData("zero")]
    [InlineData("outside")]
    [InlineData("stale")]
    [InlineData("future")]
    public async Task Invalid_GPS_rejected(string reason)
    {
        using var f = new Fixture(); var p = Position(); switch (reason) { case "latitude": p.Latitude = 91; break; case "longitude": p.Longitude = 181; break; case "missing": p.Latitude = null; break; case "accuracy": p.AccuracyMeters = 101; break; case "zero": p.AccuracyMeters = 0; break; case "outside": p.Latitude = .01m; break; case "stale": p.ClientTimestamp = f.Clock.Now.AddSeconds(-121); break; case "future": p.ClientTimestamp = f.Clock.Now.AddMinutes(1); break; }
        Assert.Equal(HttpStatusCode.BadRequest, (await f.Client.PostAsJsonAsync("/api/attendance/check-in", p)).StatusCode);
    }
    [Theory]
    [InlineData("inactive")]
    [InlineData("deleted")]
    [InlineData("locationInactive")]
    [InlineData("locationMissingCoordinates")]
    [InlineData("missingLocation")]
    [InlineData("unassignedLocation")]
    [InlineData("unassignedShift")]
    [InlineData("missingShift")]
    public async Task Invalid_employee_or_configuration_rejected(string reason)
    {
        using var f = new Fixture(); f.Change(db =>
        {
            var employee = db.Employees.Single(); var location = db.WorkLocations.Single(l => l.Id == f.LocationId);
            switch (reason)
            {
                case "inactive": employee.IsActive = false; break;
                case "deleted": db.Remove(employee); break;
                case "locationInactive": location.IsActive = false; break;
                case "locationMissingCoordinates": location.Latitude = null; location.Longitude = null; break;
                // Soft-deleted assignments represent unavailable relationships without bypassing the required FK schema.
                case "missingLocation": db.Database.ExecuteSqlInterpolated($"UPDATE WorkLocations SET IsDeleted = 1 WHERE Id = {f.LocationId}"); break;
                case "missingShift": db.Shifts.First().IsActive = false; break;
                // Simulate legacy corrupt assignments; ordinary writes cannot bypass required foreign keys.
                case "unassignedLocation":
                    db.Database.ExecuteSqlRaw("PRAGMA foreign_keys=OFF");
                    db.Database.ExecuteSqlInterpolated($"UPDATE Employees SET WorkLocationId={Guid.Empty} WHERE Id={f.EmployeeId}");
                    db.Database.ExecuteSqlRaw("PRAGMA foreign_keys=ON");
                    break;
                case "unassignedShift":
                    db.Database.ExecuteSqlRaw("PRAGMA foreign_keys=OFF");
                    db.Database.ExecuteSqlInterpolated($"UPDATE Employees SET ShiftId={Guid.Empty} WHERE Id={f.EmployeeId}");
                    db.Database.ExecuteSqlRaw("PRAGMA foreign_keys=ON");
                    break;
            }
        });
        var response = await f.Client.PostAsJsonAsync("/api/attendance/check-in", Position()); Assert.Contains(response.StatusCode, new[] { HttpStatusCode.BadRequest, HttpStatusCode.Conflict, HttpStatusCode.Forbidden });
    }
    [Fact]
    public async Task Disabled_geofence_still_validates_GPS_but_has_no_distance_enforcement()
    { using var f = new Fixture(); f.Change(db => { var l = db.WorkLocations.Single(x => x.Id == f.LocationId); l.IsGeoFenceEnabled = false; l.Latitude = null; l.Longitude = null; }); var r = await In(f); Assert.Null(r.WithinGeofence); Assert.Null(r.DistanceMeters); }
    [Fact]
    public async Task Exact_boundary_is_accepted_and_just_outside_is_rejected()
    { using (var f = new Fixture(new FixedDistance(100))) { Assert.True((await In(f)).WithinGeofence); } using (var f = new Fixture(new FixedDistance(100.000001))) { Assert.Equal(HttpStatusCode.BadRequest, (await f.Client.PostAsJsonAsync("/api/attendance/check-in", Position())).StatusCode); } }
    [Fact]
    public async Task Checkout_without_checkin_and_outside_checkout_are_rejected()
    { using var f = new Fixture(); Assert.Equal(HttpStatusCode.Conflict, (await f.Client.PostAsJsonAsync("/api/attendance/check-out", Position())).StatusCode); await In(f); Assert.Equal(HttpStatusCode.BadRequest, (await f.Client.PostAsJsonAsync("/api/attendance/check-out", Position(.01m))).StatusCode); }
    [Fact]
    public async Task Anonymous_missing_identity_and_forged_employee_are_rejected()
    {
        using var f = new Fixture(); using var anonymous = f.Factory.CreateCompanyClient(false); Assert.Equal(HttpStatusCode.Unauthorized, (await anonymous.PostAsJsonAsync("/api/attendance/check-in", Position())).StatusCode);
        using var missing = f.Factory.CreateEmployeeClient("Employee", CompanySeedData.LcapId); Assert.Equal(HttpStatusCode.Forbidden, (await missing.PostAsJsonAsync("/api/attendance/check-in", Position())).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await f.Client.PostAsJsonAsync("/api/attendance/check-in", new { latitude = 0, longitude = 0, accuracyMeters = 10, employeeId = Guid.NewGuid() })).StatusCode);
        using var wrongCompany = f.Factory.CreateEmployeeClient("Employee", Guid.NewGuid(), f.EmployeeId); Assert.Equal(HttpStatusCode.Forbidden, (await wrongCompany.PostAsJsonAsync("/api/attendance/check-in", Position())).StatusCode);
    }
    [Fact]
    public async Task Admin_reads_are_scoped_and_employees_cannot_read_others()
    {
        using var f = new Fixture(); var r = await In(f); using var foreign = f.Factory.CreateEmployeeClient("HRAdmin", Guid.NewGuid());
        Assert.Empty((await foreign.GetFromJsonAsync<ApiResponse<AttendanceResponse[]>>("/api/attendance"))!.Data!);
        Assert.Equal(HttpStatusCode.Forbidden, (await foreign.GetAsync("/api/attendance/" + r.AttendanceId)).StatusCode);
        using var hr = f.Factory.CreateEmployeeClient("HRAdmin", CompanySeedData.LcapId); Assert.Single((await hr.GetFromJsonAsync<ApiResponse<AttendanceResponse[]>>("/api/attendance"))!.Data!);
        using var other = f.Factory.CreateEmployeeClient("Employee", CompanySeedData.LcapId, Guid.NewGuid()); Assert.Equal(HttpStatusCode.Forbidden, (await other.GetAsync("/api/attendance/" + r.AttendanceId)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await f.Client.GetAsync("/api/attendance")).StatusCode);
    }
    [Fact]
    public async Task Overnight_checkout_retains_original_attendance_date()
    {
        using var f = new Fixture(); f.Change(db => { var s = db.Shifts.First(); s.StartTime = new(22, 0); s.EndTime = new(6, 0); s.IsNightShift = true; });
        f.Clock.Now = new(2026, 9, 7, 16, 30, 0, TimeSpan.Zero); var r = await In(f); Assert.Equal(new DateOnly(2026, 9, 7), r.AttendanceDate);
        f.Clock.Now = new(2026, 9, 8, 0, 30, 0, TimeSpan.Zero);
        var response = await f.Client.PostAsJsonAsync("/api/attendance/check-out", Position()); Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var done = (await response.Content.ReadFromJsonAsync<ApiResponse<AttendanceResponse>>())!.Data!; Assert.Equal(r.AttendanceId, done.AttendanceId); Assert.Equal(r.AttendanceDate, done.AttendanceDate);
    }
    [Fact]
    public async Task Soft_deleted_attendance_reserves_date_and_unique_index_is_enforced()
    {
        using var f = new Fixture(); var r = await In(f); f.Change(db => db.Remove(db.AttendanceRecords.Single()));
        Assert.Equal(HttpStatusCode.Conflict, (await f.Client.PostAsJsonAsync("/api/attendance/check-in", Position())).StatusCode);
        Assert.Throws<DbUpdateException>(() => f.Change(db => { var copy = db.AttendanceRecords.IgnoreQueryFilters().AsNoTracking().Single(); copy.Id = Guid.NewGuid(); copy.CheckOutTime = f.Clock.Now; copy.Status = AttendanceStatus.Completed; db.Add(copy); }));
    }
}



