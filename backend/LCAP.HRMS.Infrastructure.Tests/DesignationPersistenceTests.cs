using LCAP.HRMS.Application.Abstractions;
using LCAP.HRMS.Domain.Designations;
using LCAP.HRMS.Infrastructure.Persistence;
using LCAP.HRMS.Infrastructure.Persistence.Seeds;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace LCAP.HRMS.Infrastructure.Tests;

public sealed class DesignationPersistenceTests : IDisposable
{
    private readonly SqliteConnection _connection = new("Data Source=:memory:");
    private readonly ApplicationDbContext _context;

    public DesignationPersistenceTests()
    {
        _connection.Open();
        _connection.CreateCollation("Latin1_General_100_CI_AS",
            (left, right) => string.Compare(left, right, StringComparison.OrdinalIgnoreCase));
        _context = new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite(_connection).Options, new TestUser(), TimeProvider.System);
        _context.Database.EnsureCreated();
    }

    [Fact]
    public async Task Database_rejects_duplicate_company_code_pair()
    {
        _context.Add(new Designation { CompanyId = CompanySeedData.LcapId, DesignationCode = "exec", DesignationName = "Duplicate" });
        await Assert.ThrowsAsync<DbUpdateException>(() => _context.SaveChangesAsync());
    }

    [Fact]
    public async Task Foreign_key_rejects_nonexistent_company()
    {
        _context.Add(new Designation { CompanyId = Guid.NewGuid(), DesignationCode = "INVALID", DesignationName = "Invalid" });
        await Assert.ThrowsAsync<DbUpdateException>(() => _context.SaveChangesAsync());
    }

    [Fact]
    public void Sql_server_model_has_nullable_level_default_false_and_unique_index()
    {
        using var context = new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlServer("Server=unused;Database=unused;Integrated Security=True;").Options, new TestUser(), TimeProvider.System);
        var entity = context.Model.FindEntityType(typeof(Designation))!;
        Assert.Equal("int", entity.FindProperty(nameof(Designation.Level))!.GetColumnType());
        Assert.True(entity.FindProperty(nameof(Designation.Level))!.IsNullable);
        Assert.Equal(false, entity.FindProperty(nameof(Designation.IsManagerial))!.GetDefaultValue());
        Assert.Contains(entity.GetIndexes(), index => index.IsUnique &&
            index.Properties.Select(property => property.Name).SequenceEqual(new[] { "CompanyId", "DesignationCode" }));
        Assert.Equal(DeleteBehavior.Restrict, Assert.Single(entity.GetForeignKeys()).DeleteBehavior);
    }

    public void Dispose()
    {
        _context.Dispose();
        _connection.Dispose();
    }

    private sealed class TestUser : ICurrentUser
    {
        public string? UserId => "designation-test-user";
    }
}
