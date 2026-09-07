using System.ComponentModel.DataAnnotations;
using LCAP.HRMS.Application.Abstractions;
using LCAP.HRMS.Application.Companies;
using LCAP.HRMS.Application.Companies.DTOs;
using LCAP.HRMS.Domain.Companies;
using LCAP.HRMS.Infrastructure.Persistence;
using LCAP.HRMS.Infrastructure.Persistence.Repositories;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace LCAP.HRMS.Infrastructure.Tests;

public sealed class CompanyPersistenceTests : IDisposable
{
    private readonly SqliteConnection _connection = new("Data Source=:memory:");
    private readonly ApplicationDbContext _context;

    public CompanyPersistenceTests()
    {
        _connection.Open();
        _connection.CreateCollation("Latin1_General_100_CI_AS",
            (left, right) => string.Compare(left, right, StringComparison.OrdinalIgnoreCase));
        _context = new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite(_connection).Options, new TestUser(), TimeProvider.System);
        _context.Database.EnsureCreated();
    }

    [Fact]
    public async Task Database_rejects_duplicate_code_without_a_service_precheck()
    {
        // Existing LCAP was inserted by model seed. Simulates a write that races past the precheck.
        _context.Companies.Add(new Company { CompanyCode = "lcap", CompanyName = "Duplicate", PayrollDay = 1, SalaryPaymentDay = 1 });
        await Assert.ThrowsAsync<DbUpdateException>(() => _context.SaveChangesAsync());
    }

    [Theory]
    [InlineData(0, 1)]
    [InlineData(32, 1)]
    [InlineData(1, 0)]
    [InlineData(1, 32)]
    public async Task Database_enforces_day_constraints(int payrollDay, int salaryPaymentDay)
    {
        _context.Companies.Add(new Company { CompanyCode = "INVALID", CompanyName = "Invalid",
            PayrollDay = payrollDay, SalaryPaymentDay = salaryPaymentDay });
        await Assert.ThrowsAsync<DbUpdateException>(() => _context.SaveChangesAsync());
    }

    [Fact]
    public async Task Service_validates_when_called_without_MVC()
    {
        var service = new CompanyService(new CompanyRepository(_context), _context);
        await Assert.ThrowsAsync<ValidationException>(() => service.CreateAsync(
            new CompanyCreateRequest { CompanyCode = "BAD", CompanyName = "Bad", PayrollDay = 32, SalaryPaymentDay = 1 }));
        Assert.Single(await _context.Companies.ToListAsync());
    }

    public void Dispose()
    {
        _context.Dispose();
        _connection.Dispose();
    }

    private sealed class TestUser : ICurrentUser
    {
        public string? UserId => "persistence-test-user";
    }
}
