using LCAP.HRMS.Application.Abstractions;
using LCAP.HRMS.Application.Abstractions.Persistence;
using LCAP.HRMS.Domain.Common;
using LCAP.HRMS.Infrastructure.Persistence;
using LCAP.HRMS.Infrastructure.Persistence.Repositories;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace LCAP.HRMS.Infrastructure.Tests;

public sealed class FoundationTests : IDisposable
{
    private readonly SqliteConnection _connection = new("Data Source=:memory:");
    private readonly TestUser _user = new();
    private readonly TestClock _clock = new();
    private readonly TestContext _context;

    public FoundationTests()
    {
        _connection.Open();
        _connection.CreateCollation("Latin1_General_100_CI_AS", (left, right) =>
            string.Compare(left, right, StringComparison.OrdinalIgnoreCase));
        _context = new TestContext(new DbContextOptionsBuilder<TestContext>().UseSqlite(_connection).Options, _user, _clock);
        _context.Database.EnsureCreated(); // Only the disposable in-memory test database.
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Save_populates_audit_fields_and_preserves_creator(bool async)
    {
        var entity = new TestRecord { Name = "Original", CreatedBy = "spoofed", IsDeleted = true };
        _context.Add(entity);
        if (async) await _context.SaveChangesAsync(); else _context.SaveChanges();
        Assert.NotEqual(Guid.Empty, entity.Id);
        Assert.Equal(_clock.Now, entity.CreatedAt);
        Assert.Equal("creator", entity.CreatedBy);
        Assert.Null(entity.UpdatedAt);
        Assert.False(entity.IsDeleted);

        var createdAt = entity.CreatedAt;
        _clock.Now = _clock.Now.AddHours(1);
        _user.UserId = "editor";
        entity.Name = "Edited";
        entity.CreatedAt = DateTimeOffset.MinValue;
        entity.CreatedBy = "overwritten";
        if (async) await _context.SaveChangesAsync(true); else _context.SaveChanges(true);

        _context.ChangeTracker.Clear();
        var stored = await _context.Set<TestRecord>().SingleAsync();
        Assert.Equal(createdAt, stored.CreatedAt);
        Assert.Equal("creator", stored.CreatedBy);
        Assert.Equal(_clock.Now, stored.UpdatedAt);
        Assert.Equal("editor", stored.UpdatedBy);
        Assert.Equal("Edited", stored.Name);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Delete_updates_row_and_global_filter_hides_it(bool async)
    {
        var repository = new GenericRepository<TestRecord>(_context);
        var entity = new TestRecord { Name = "Retained" };
        await repository.AddAsync(entity);
        await _context.SaveChangesAsync();
        _clock.Now = _clock.Now.AddDays(1);
        _user.UserId = "deleter";

        repository.Remove(entity);
        if (async) await _context.SaveChangesAsync(); else _context.SaveChanges();
        Assert.Null(await repository.GetByIdAsync(entity.Id));
        Assert.Empty(await repository.ListAsync());
        Assert.Empty(await _context.Set<TestRecord>().ToListAsync());
        _context.ChangeTracker.Clear();
        var deleted = await _context.Set<TestRecord>().IgnoreQueryFilters().SingleAsync();
        Assert.True(deleted.IsDeleted);
        Assert.Equal("Retained", deleted.Name);
        Assert.Equal("creator", deleted.CreatedBy);
        Assert.Equal("deleter", deleted.UpdatedBy);
        Assert.Equal(_clock.Now, deleted.UpdatedAt);
    }

    [Fact]
    public async Task Detached_delete_does_not_overwrite_data_or_creation_audit()
    {
        var entity = new TestRecord { Name = "Keep me" };
        _context.Add(entity);
        await _context.SaveChangesAsync();
        var createdAt = entity.CreatedAt;
        _context.ChangeTracker.Clear();

        _context.Remove(new TestRecord { Id = entity.Id });
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        var stored = await _context.Set<TestRecord>().IgnoreQueryFilters().SingleAsync();
        Assert.True(stored.IsDeleted);
        Assert.Equal("Keep me", stored.Name);
        Assert.Equal(createdAt, stored.CreatedAt);
        Assert.Equal("creator", stored.CreatedBy);
    }

    [Fact]
    public async Task Detached_update_preserves_creation_audit()
    {
        var entity = new TestRecord { Name = "Original" };
        _context.Add(entity);
        await _context.SaveChangesAsync();
        var createdAt = entity.CreatedAt;
        _context.ChangeTracker.Clear();

        _context.Update(new TestRecord { Id = entity.Id, Name = "Detached update", CreatedBy = "spoofed" });
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();
        var stored = await _context.Set<TestRecord>().SingleAsync();
        Assert.Equal(createdAt, stored.CreatedAt);
        Assert.Equal("creator", stored.CreatedBy);
        Assert.Equal("Detached update", stored.Name);
    }

    [Fact]
    public async Task Repositories_defer_writes_and_share_one_commit()
    {
        var firstRepository = new GenericRepository<TestRecord>(_context);
        var secondRepository = new GenericRepository<OtherRecord>(_context);
        await firstRepository.AddAsync(new TestRecord { Name = "First" });
        await secondRepository.AddAsync(new OtherRecord());
        Assert.Equal(0, await _context.Set<TestRecord>().CountAsync());
        Assert.Equal(0, await _context.Set<OtherRecord>().CountAsync());

        IUnitOfWork unitOfWork = _context;
        Assert.Equal(2, await unitOfWork.SaveChangesAsync());
        Assert.Single(await firstRepository.ListAsync());
        Assert.Single(await secondRepository.ListAsync());
    }

    [Fact]
    public async Task Read_by_id_tracks_updates_while_list_is_read_only_and_bounded()
    {
        _context.AddRange(new TestRecord { Name = "One" }, new TestRecord { Name = "Two" });
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();
        var repository = new GenericRepository<TestRecord>(_context);
        var page = await repository.ListAsync(take: 1);
        Assert.Single(page);
        Assert.Empty(_context.ChangeTracker.Entries());

        var tracked = await repository.GetByIdAsync(page[0].Id);
        tracked!.Name = "Changed";
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();
        Assert.Single(await repository.ListAsync(predicate: row => row.Name == "Changed"));
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => repository.ListAsync(take: 0));
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => repository.ListAsync(skip: -1));
    }

    [Fact]
    public async Task Background_execution_allows_unknown_actor()
    {
        _user.UserId = null;
        var entity = new TestRecord { Name = "Background" };
        _context.Add(entity);
        await _context.SaveChangesAsync();
        Assert.Null(entity.CreatedBy);
        Assert.Equal(_clock.Now, entity.CreatedAt);
    }

    [Fact]
    public void Sql_server_model_has_expected_foundation_mapping()
    {
        using var sqlContext = new TestContext(new DbContextOptionsBuilder<TestContext>()
            .UseSqlServer("Server=unused;Database=unused;Integrated Security=True;").Options, _user, _clock);
        var entity = sqlContext.Model.FindEntityType(typeof(TestRecord))!;
        Assert.Equal("dbo", entity.GetSchema());
        Assert.Equal("uniqueidentifier", entity.FindProperty(nameof(BaseEntity.Id))!.GetColumnType());
        Assert.Equal("datetimeoffset(7)", entity.FindProperty(nameof(BaseEntity.CreatedAt))!.GetColumnType());
        Assert.Equal(200, entity.FindProperty(nameof(BaseEntity.CreatedBy))!.GetMaxLength());
        Assert.Equal("bit", entity.FindProperty(nameof(BaseEntity.IsDeleted))!.GetColumnType());
        Assert.Contains("[dbo].[TestRecords]", sqlContext.Set<TestRecord>().ToQueryString());
        Assert.Contains("[IsDeleted] = CAST(0 AS bit)", sqlContext.Set<TestRecord>().ToQueryString());
    }

    public void Dispose()
    {
        _context.Dispose();
        _connection.Dispose();
    }

    private sealed class TestContext(DbContextOptions<TestContext> options, ICurrentUser user, TimeProvider clock)
        : ApplicationDbContext(options, user, clock)
    {
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<TestRecord>().ToTable("TestRecords");
            modelBuilder.Entity<OtherRecord>().ToTable("OtherRecords");
            base.OnModelCreating(modelBuilder);
        }
    }

    private sealed class TestRecord : BaseEntity
    {
        public string Name { get; set; } = "";
    }

    private sealed class OtherRecord : BaseEntity;

    private sealed class TestUser : ICurrentUser
    {
        public string? UserId { get; set; } = "creator";
    }

    private sealed class TestClock : TimeProvider
    {
        public DateTimeOffset Now { get; set; } = new(2026, 9, 7, 0, 0, 0, TimeSpan.Zero);
        public override DateTimeOffset GetUtcNow() => Now;
    }
}
