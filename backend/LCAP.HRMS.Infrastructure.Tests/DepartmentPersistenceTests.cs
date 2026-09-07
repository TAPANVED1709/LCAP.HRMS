using LCAP.HRMS.Application.Abstractions;
using LCAP.HRMS.Domain.Departments;
using LCAP.HRMS.Domain.Companies;
using LCAP.HRMS.Infrastructure.Persistence;
using LCAP.HRMS.Infrastructure.Persistence.Seeds;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace LCAP.HRMS.Infrastructure.Tests;

public sealed class DepartmentPersistenceTests : IDisposable
{
    private readonly SqliteConnection _connection = new("Data Source=:memory:");
    private readonly ApplicationDbContext _context;

    public DepartmentPersistenceTests()
    {
        _connection.Open();
        _connection.CreateCollation("Latin1_General_100_CI_AS",
            (left, right) => string.Compare(left, right, StringComparison.OrdinalIgnoreCase));
        _context = new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite(_connection).Options, new TestUser(), TimeProvider.System);
        _context.Database.EnsureCreated();
    }

    [Fact]
    public async Task Database_enforces_per_company_uniqueness()
    {
        _context.Departments.Add(new Department { CompanyId = CompanySeedData.LcapId, DepartmentCode = "hr", DepartmentName = "Duplicate" });
        await Assert.ThrowsAsync<DbUpdateException>(() => _context.SaveChangesAsync());
    }

    [Fact]
    public async Task Database_rejects_self_parent()
    {
        var department = new Department { CompanyId = CompanySeedData.LcapId, DepartmentCode = "SELF", DepartmentName = "Self" };
        _context.Add(department);
        await _context.SaveChangesAsync();
        department.ParentDepartmentId = department.Id;
        await Assert.ThrowsAsync<DbUpdateException>(() => _context.SaveChangesAsync());
    }

    [Fact]
    public async Task Composite_foreign_key_rejects_cross_company_parent()
    {
        var other = new Company { CompanyCode = "OTHER", CompanyName = "Other", PayrollDay = 1, SalaryPaymentDay = 1 };
        _context.Add(other);
        await _context.SaveChangesAsync();
        var hr = await _context.Departments.SingleAsync(item => item.DepartmentCode == "HR");
        _context.Add(new Department { CompanyId = other.Id, DepartmentCode = "CROSS", DepartmentName = "Cross", ParentDepartmentId = hr.Id });
        await Assert.ThrowsAsync<DbUpdateException>(() => _context.SaveChangesAsync());
    }

    [Fact]
    public async Task Foreign_keys_reject_nonexistent_company_and_parent()
    {
        _context.Add(new Department { CompanyId = Guid.NewGuid(), DepartmentCode = "INVALID", DepartmentName = "Invalid" });
        await Assert.ThrowsAsync<DbUpdateException>(() => _context.SaveChangesAsync());
        _context.ChangeTracker.Clear();
        _context.Add(new Department { CompanyId = CompanySeedData.LcapId, DepartmentCode = "INVALID", DepartmentName = "Invalid", ParentDepartmentId = Guid.NewGuid() });
        await Assert.ThrowsAsync<DbUpdateException>(() => _context.SaveChangesAsync());
    }

    public void Dispose()
    {
        _context.Dispose();
        _connection.Dispose();
    }

    private sealed class TestUser : ICurrentUser
    {
        public string? UserId => "department-test-user";
    }
}
