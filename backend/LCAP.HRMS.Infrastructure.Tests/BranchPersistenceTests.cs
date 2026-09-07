using LCAP.HRMS.Application.Abstractions;
using LCAP.HRMS.Domain.Branches;
using LCAP.HRMS.Infrastructure.Persistence;
using LCAP.HRMS.Infrastructure.Persistence.Seeds;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace LCAP.HRMS.Infrastructure.Tests;

public sealed class BranchPersistenceTests : IDisposable
{
    private readonly SqliteConnection _connection = new("Data Source=:memory:");
    private readonly ApplicationDbContext _context;

    public BranchPersistenceTests()
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
        _context.Branches.Add(new Branch { CompanyId = CompanySeedData.LcapId, BranchCode = "patna-ho", BranchName = "Duplicate" });
        await Assert.ThrowsAsync<DbUpdateException>(() => _context.SaveChangesAsync());
    }

    [Fact]
    public async Task Foreign_key_rejects_nonexistent_company()
    {
        _context.Branches.Add(new Branch { CompanyId = Guid.NewGuid(), BranchCode = "INVALID", BranchName = "Invalid" });
        await Assert.ThrowsAsync<DbUpdateException>(() => _context.SaveChangesAsync());
    }

    [Fact]
    public void Sql_server_model_has_composite_unique_index_restrict_foreign_key_and_parent_filter()
    {
        using var sqlContext = new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlServer("Server=unused;Database=unused;Integrated Security=True;").Options, new TestUser(), TimeProvider.System);
        var entity = sqlContext.Model.FindEntityType(typeof(Branch))!;
        Assert.Contains(entity.GetIndexes(), index => index.IsUnique &&
            index.Properties.Select(property => property.Name).SequenceEqual(new[] { "CompanyId", "BranchCode" }));
        Assert.Equal(DeleteBehavior.Restrict, Assert.Single(entity.GetForeignKeys()).DeleteBehavior);
        Assert.Equal("dbo", entity.GetSchema());
        var sql = sqlContext.Branches.ToQueryString();
        Assert.Contains("[dbo].[Companies]", sql);
        Assert.Contains("[dbo].[Branches]", sql);
        Assert.Contains("[IsDeleted]", sql);
    }

    public void Dispose()
    {
        _context.Dispose();
        _connection.Dispose();
    }

    private sealed class TestUser : ICurrentUser
    {
        public string? UserId => "branch-test-user";
    }
}
