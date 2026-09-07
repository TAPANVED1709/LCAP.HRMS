using LCAP.HRMS.Application.Abstractions;
using LCAP.HRMS.Domain.WorkLocations;
using LCAP.HRMS.Infrastructure.Persistence;
using LCAP.HRMS.Infrastructure.Persistence.Seeds;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace LCAP.HRMS.Infrastructure.Tests;

public sealed class WorkLocationPersistenceTests : IDisposable
{
    private readonly SqliteConnection _connection = new("Data Source=:memory:");
    private readonly ApplicationDbContext _context;

    public WorkLocationPersistenceTests()
    {
        _connection.Open();
        _connection.CreateCollation("Latin1_General_100_CI_AS",
            (left, right) => string.Compare(left, right, StringComparison.OrdinalIgnoreCase));
        _context = new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite(_connection).Options, new TestUser(), TimeProvider.System);
        _context.Database.EnsureCreated();
    }

    private static WorkLocation Valid() => new()
    {
        CompanyId = CompanySeedData.LcapId, BranchId = BranchSeedData.PatnaHeadOfficeId,
        LocationCode = "TEST", LocationName = "Test"
    };

    [Theory]
    [InlineData(91, 0, 100)]
    [InlineData(-91, 0, 100)]
    [InlineData(0, 181, 100)]
    [InlineData(0, -181, 100)]
    [InlineData(0, 0, 0)]
    [InlineData(0, 0, -1)]
    public async Task Database_rejects_invalid_bounds_and_radius(int latitude, int longitude, int radius)
    {
        var location = Valid();
        location.Latitude = latitude;
        location.Longitude = longitude;
        location.AllowedRadiusMeters = radius;
        _context.Add(location);
        await Assert.ThrowsAsync<DbUpdateException>(() => _context.SaveChangesAsync());
    }

    [Fact]
    public async Task Nullable_seed_defaults_and_explicit_false_are_preserved()
    {
        var seed = await _context.WorkLocations.SingleAsync();
        Assert.Null(seed.Latitude);
        Assert.Null(seed.Longitude);
        Assert.Equal(100, seed.AllowedRadiusMeters);
        Assert.True(seed.IsGeoFenceEnabled);
        var location = Valid();
        location.IsGeoFenceEnabled = false;
        _context.Add(location);
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();
        Assert.False((await _context.WorkLocations.SingleAsync(item => item.Id == location.Id)).IsGeoFenceEnabled);
    }

    [Fact]
    public async Task Database_enforces_unique_code_branch_fk_and_coordinate_pair()
    {
        var location = Valid();
        location.LocationCode = "patna-office";
        _context.Add(location);
        await Assert.ThrowsAsync<DbUpdateException>(() => _context.SaveChangesAsync());
        _context.ChangeTracker.Clear();
        location = Valid();
        location.BranchId = Guid.NewGuid();
        _context.Add(location);
        await Assert.ThrowsAsync<DbUpdateException>(() => _context.SaveChangesAsync());
        _context.ChangeTracker.Clear();
        location = Valid();
        location.Latitude = 0;
        _context.Add(location);
        await Assert.ThrowsAsync<DbUpdateException>(() => _context.SaveChangesAsync());
    }

    [Fact]
    public void Sql_server_mapping_has_requested_precision_and_defaults()
    {
        using var context = new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlServer("Server=unused;Database=unused;Integrated Security=True;").Options, new TestUser(), TimeProvider.System);
        var entity = context.Model.FindEntityType(typeof(WorkLocation))!;
        foreach (var name in new[] { "Latitude", "Longitude" })
        {
            var property = entity.FindProperty(name)!;
            Assert.True(property.IsNullable);
            Assert.Equal(10, property.GetPrecision());
            Assert.Equal(7, property.GetScale());
            Assert.Equal("decimal(10,7)", property.GetColumnType());
        }
        Assert.Equal(100, entity.FindProperty("AllowedRadiusMeters")!.GetDefaultValue());
        Assert.Equal(true, entity.FindProperty("IsGeoFenceEnabled")!.GetDefaultValue());
    }

    public void Dispose()
    {
        _context.Dispose();
        _connection.Dispose();
    }

    private sealed class TestUser : ICurrentUser
    {
        public string? UserId => "location-test-user";
    }
}
