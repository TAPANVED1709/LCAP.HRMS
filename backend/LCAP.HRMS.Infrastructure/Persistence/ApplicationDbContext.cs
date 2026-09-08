using LCAP.HRMS.Domain.Regularisation;
using LCAP.HRMS.Domain.AttendancePolicies;
using LCAP.HRMS.Domain.Shifts;
using System.Reflection;
using LCAP.HRMS.Domain.Branches;
using LCAP.HRMS.Domain.Departments;
using LCAP.HRMS.Domain.Designations;
using LCAP.HRMS.Domain.WorkLocations;
using LCAP.HRMS.Domain.Companies;
using LCAP.HRMS.Application.Common.Exceptions;
using Microsoft.Data.SqlClient;
using LCAP.HRMS.Application.Abstractions;
using LCAP.HRMS.Application.Abstractions.Persistence;
using LCAP.HRMS.Domain.Common;
using LCAP.HRMS.Infrastructure.Persistence.Configurations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace LCAP.HRMS.Infrastructure.Persistence;
public class ApplicationDbContext : DbContext, IUnitOfWork
{
    public DbSet<LCAP.HRMS.Domain.Employees.Employee> Employees => Set<LCAP.HRMS.Domain.Employees.Employee>();
    public DbSet<LCAP.HRMS.Domain.Attendance.AttendanceRecord> AttendanceRecords => Set<LCAP.HRMS.Domain.Attendance.AttendanceRecord>();
    public DbSet<AttendancePolicy> AttendancePolicies => Set<AttendancePolicy>();
    public DbSet<AttendanceEvaluation> AttendanceEvaluations => Set<AttendanceEvaluation>();
    public DbSet<AttendancePenaltyEvent> AttendancePenaltyEvents => Set<AttendancePenaltyEvent>();
    public DbSet<AttendanceRegularisationRequest> AttendanceRegularisationRequests => Set<AttendanceRegularisationRequest>();
    public DbSet<AttendanceCorrection> AttendanceCorrections => Set<AttendanceCorrection>();
    public DbSet<AttendanceEvaluationRevision> AttendanceEvaluationRevisions => Set<AttendanceEvaluationRevision>();
    public DbSet<RegularisationAuditEntry> RegularisationAuditEntries => Set<RegularisationAuditEntry>();
    public DbSet<Shift> Shifts => Set<Shift>();
    public DbSet<Company> Companies => Set<Company>();
    public DbSet<Branch> Branches => Set<Branch>();
    public DbSet<Department> Departments => Set<Department>();
    public DbSet<Designation> Designations => Set<Designation>();
    public DbSet<WorkLocation> WorkLocations => Set<WorkLocation>();

    private readonly ICurrentUser _currentUser;
    private readonly TimeProvider _timeProvider;
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options, ICurrentUser currentUser, TimeProvider timeProvider) : this((DbContextOptions)options, currentUser, timeProvider)
    {
    }

    protected ApplicationDbContext(DbContextOptions options, ICurrentUser currentUser, TimeProvider timeProvider) : base(options)
    {
        _currentUser = currentUser;
        _timeProvider = timeProvider;
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.HasDefaultSchema("dbo");
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);
        ApplyFoundationConventions(modelBuilder);
        modelBuilder.Entity<Shift>().HasQueryFilter(shift => !shift.IsDeleted && !shift.Company.IsDeleted);
        modelBuilder.Entity<LCAP.HRMS.Domain.Employees.Employee>().HasQueryFilter(e => !e.IsDeleted && !e.Company.IsDeleted);
        // EF Core 8 uses one combined filter: hide deleted branches and deleted parents.
        modelBuilder.Entity<Branch>().HasQueryFilter(branch => !branch.IsDeleted && !branch.Company.IsDeleted);
        modelBuilder.Entity<Department>().HasQueryFilter(department => !department.IsDeleted && !department.Company.IsDeleted);
        modelBuilder.Entity<Designation>().HasQueryFilter(designation => !designation.IsDeleted && !designation.Company.IsDeleted);
        modelBuilder.Entity<WorkLocation>().HasQueryFilter(location => !location.IsDeleted && !location.Company.IsDeleted && !location.Branch.IsDeleted && !location.Branch.Company.IsDeleted);
    }

    // Call after registering entities in derived test contexts as well.
    protected static void ApplyFoundationConventions(ModelBuilder modelBuilder)
    {
        foreach (var entityType in modelBuilder.Model.GetEntityTypes().Where(type => typeof(BaseEntity).IsAssignableFrom(type.ClrType) && type.BaseType is null).ToArray())
        {
            typeof(ApplicationDbContext).GetMethod(nameof(ConfigureBaseEntity), BindingFlags.NonPublic | BindingFlags.Static)!.MakeGenericMethod(entityType.ClrType).Invoke(null, [modelBuilder]);
            // Preserve original creator values even for detached entities attached as Modified.
            entityType.FindProperty(nameof(BaseEntity.CreatedAt))!.SetAfterSaveBehavior(PropertySaveBehavior.Ignore);
            entityType.FindProperty(nameof(BaseEntity.CreatedBy))!.SetAfterSaveBehavior(PropertySaveBehavior.Ignore);
        }

        // No implicit hard-delete cascades across soft-deletable entities.
        foreach (var foreignKey in modelBuilder.Model.GetEntityTypes().SelectMany(type => type.GetForeignKeys()).Where(key => typeof(BaseEntity).IsAssignableFrom(key.DeclaringEntityType.ClrType) || typeof(BaseEntity).IsAssignableFrom(key.PrincipalEntityType.ClrType)))
            foreignKey.DeleteBehavior = DeleteBehavior.Restrict;
    }

    private static void ConfigureBaseEntity<T>(ModelBuilder modelBuilder)
        where T : BaseEntity => modelBuilder.ApplyConfiguration(new BaseEntityConfiguration<T>());
    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        ApplyAuditChanges();
        try
        {
            return base.SaveChanges(acceptAllChangesOnSuccess);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new ConflictException("This request changed concurrently. Refresh before retrying.");
        }
        catch (DbUpdateException exception)when (exception.InnerException is SqlException { Number: 2601 or 2627 } && exception.Entries.Any(e => e.Entity is AttendanceRegularisationRequest or AttendanceCorrection or AttendanceEvaluationRevision))
        {
            throw new ConflictException("This regularisation operation already exists. Refresh before retrying.");
        }
        catch (DbUpdateException exception)when (exception.InnerException is SqlException { Number: 2601 or 2627 } && exception.Entries.Any(e => e.Entity is LCAP.HRMS.Domain.Employees.Employee))
        {
            throw new ConflictException("EmployeeCode is already in use within this company.");
        }
        catch (DbUpdateException exception)when (exception.InnerException is SqlException { Number: 2601 or 2627 } && exception.Entries.Any(e => e.Entity is LCAP.HRMS.Domain.Attendance.AttendanceRecord))
        {
            throw new ConflictException("Attendance already exists or has already been checked in.");
        }
        catch (DbUpdateException exception)when (exception.InnerException is SqlException { Number: 2601 or 2627 } && exception.Entries.Any(e => e.Entity is AttendancePolicy))
        {
            throw new ConflictException("PolicyCode is already in use within this company.");
        }
        catch (DbUpdateException exception)when (IsDuplicateShiftCode(exception))
        {
            throw new ConflictException("ShiftCode is already in use within this company.");
        }
        catch (DbUpdateException exception)when (IsDuplicateCompanyCode(exception))
        {
            throw new ConflictException("CompanyCode is already in use.");
        }
        catch (DbUpdateException exception)when (IsDuplicateBranchCode(exception))
        {
            throw new ConflictException("BranchCode is already in use within this company.");
        }
        catch (DbUpdateException exception)when (IsDuplicateDepartmentCode(exception))
        {
            throw new ConflictException("DepartmentCode is already in use within this company.");
        }
        catch (DbUpdateException exception)when (IsDuplicateDesignationCode(exception))
        {
            throw new ConflictException("DesignationCode is already in use within this company.");
        }
        catch (DbUpdateException exception)when (IsDuplicateLocationCode(exception))
        {
            throw new ConflictException("LocationCode is already in use within this branch.");
        }
    }

    public override async Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
    {
        ApplyAuditChanges();
        try
        {
            return await base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new ConflictException("This request changed concurrently. Refresh before retrying.");
        }
        catch (DbUpdateException exception)when (exception.InnerException is SqlException { Number: 2601 or 2627 } && exception.Entries.Any(e => e.Entity is AttendanceRegularisationRequest or AttendanceCorrection or AttendanceEvaluationRevision))
        {
            throw new ConflictException("This regularisation operation already exists. Refresh before retrying.");
        }
        catch (DbUpdateException exception)when (exception.InnerException is SqlException { Number: 2601 or 2627 } && exception.Entries.Any(e => e.Entity is LCAP.HRMS.Domain.Employees.Employee))
        {
            throw new ConflictException("EmployeeCode is already in use within this company.");
        }
        catch (DbUpdateException exception)when (exception.InnerException is SqlException { Number: 2601 or 2627 } && exception.Entries.Any(e => e.Entity is LCAP.HRMS.Domain.Attendance.AttendanceRecord))
        {
            throw new ConflictException("Attendance already exists or has already been checked in.");
        }
        catch (DbUpdateException exception)when (exception.InnerException is SqlException { Number: 2601 or 2627 } && exception.Entries.Any(e => e.Entity is AttendancePolicy))
        {
            throw new ConflictException("PolicyCode is already in use within this company.");
        }
        catch (DbUpdateException exception)when (IsDuplicateShiftCode(exception))
        {
            throw new ConflictException("ShiftCode is already in use within this company.");
        }
        catch (DbUpdateException exception)when (IsDuplicateCompanyCode(exception))
        {
            throw new ConflictException("CompanyCode is already in use.");
        }
        catch (DbUpdateException exception)when (IsDuplicateBranchCode(exception))
        {
            throw new ConflictException("BranchCode is already in use within this company.");
        }
        catch (DbUpdateException exception)when (IsDuplicateDepartmentCode(exception))
        {
            throw new ConflictException("DepartmentCode is already in use within this company.");
        }
        catch (DbUpdateException exception)when (IsDuplicateDesignationCode(exception))
        {
            throw new ConflictException("DesignationCode is already in use within this company.");
        }
        catch (DbUpdateException exception)when (IsDuplicateLocationCode(exception))
        {
            throw new ConflictException("LocationCode is already in use within this branch.");
        }
    }

    // Enforces a 409 response even when concurrent requests pass the pre-insert check.
    private static bool IsDuplicateCompanyCode(DbUpdateException exception) => exception.InnerException is SqlException { Number: 2601 or 2627 } && exception.Entries.Any(entry => entry.Entity is Company);
    private static bool IsDuplicateBranchCode(DbUpdateException exception) => exception.InnerException is SqlException { Number: 2601 or 2627 } && exception.Entries.Any(entry => entry.Entity is Branch);
    private static bool IsDuplicateDepartmentCode(DbUpdateException exception) => exception.InnerException is SqlException { Number: 2601 or 2627 } && exception.Entries.Any(entry => entry.Entity is Department);
    private static bool IsDuplicateDesignationCode(DbUpdateException exception) => exception.InnerException is SqlException { Number: 2601 or 2627 } && exception.Entries.Any(entry => entry.Entity is Designation);
    private static bool IsDuplicateLocationCode(DbUpdateException exception) => exception.InnerException is SqlException { Number: 2601 or 2627 } && exception.Entries.Any(entry => entry.Entity is WorkLocation);
    private static bool IsDuplicateShiftCode(DbUpdateException exception) => exception.InnerException is SqlException { Number: 2601 or 2627 } && exception.Entries.Any(entry => entry.Entity is Shift);
    private void ApplyAuditChanges()
    {
        ChangeTracker.DetectChanges();
        if (ChangeTracker.Entries().Any(e => e.Entity is AttendanceEvaluation or AttendancePenaltyEvent or AttendanceCorrection or AttendanceEvaluationRevision or RegularisationAuditEntry && e.State is EntityState.Modified or EntityState.Deleted))
            throw new ConflictException("Attendance evaluation and penalty history cannot be changed.");
        foreach (var request in ChangeTracker.Entries<AttendanceRegularisationRequest>().Where(e => e.State is EntityState.Modified or EntityState.Deleted))
        {
            var old = request.OriginalValues.GetValue<RegularisationStatus>("Status");
            var next = request.Entity.Status;
            string[] mutable = ["Status", "ReviewedAt", "ReviewedByEmployeeId", "ReviewerRemarks", "AppliedAt", "AppliedBy", "CancellationReason", "UpdatedAt", "UpdatedBy"];
            if (request.State == EntityState.Deleted || request.Properties.Any(p => p.IsModified && !mutable.Contains(p.Metadata.Name)) || !(old == RegularisationStatus.PendingManager && next is RegularisationStatus.Approved or RegularisationStatus.Rejected or RegularisationStatus.Cancelled || old == RegularisationStatus.Approved && next == RegularisationStatus.Applied))
                throw new ConflictException("Regularisation history or transition cannot be changed.");
        }

        foreach (var employee in ChangeTracker.Entries<LCAP.HRMS.Domain.Employees.Employee>().Where(e => e.State == EntityState.Modified && e.Properties.Any(p => p.Metadata.Name is "WorkLocationId" or "ShiftId" && p.IsModified && !Equals(p.OriginalValue, p.CurrentValue))))
        {
            if (AttendanceRecords.IgnoreQueryFilters().Any(r => r.EmployeeId == employee.Entity.Id && !r.IsDeleted && r.CheckOutTime == null && !AttendanceCorrections.Any(c => c.AttendanceRecordId == r.Id && c.CorrectedCheckOutTime != null)))
                throw new ConflictException("Complete open attendance before changing the work location or shift.");
        }

        // Preserve assignments when a Day 1 master is deleted or moved after employee creation.
        foreach (var entry in ChangeTracker.Entries<BaseEntity>().ToArray())
        {
            var removing = entry.State == EntityState.Deleted || (entry.State == EntityState.Modified && entry.Property(nameof(BaseEntity.IsDeleted)).IsModified && entry.Entity.IsDeleted);
            var moving = entry.State == EntityState.Modified && entry.Properties.Any(p => p.Metadata.Name is "CompanyId" or "BranchId" && p.IsModified && !Equals(p.OriginalValue, p.CurrentValue));
            if (!removing && !moving)
                continue;
            var id = entry.Entity.Id;
            var query = Employees.IgnoreQueryFilters();
            var referenced = entry.Entity switch
            {
                Company => query.Any(e => e.CompanyId == id),
                Branch => query.Any(e => e.BranchId == id),
                Department => query.Any(e => e.DepartmentId == id),
                Designation => query.Any(e => e.DesignationId == id),
                Shift => query.Any(e => e.ShiftId == id),
                WorkLocation => query.Any(e => e.WorkLocationId == id),
                _ => false
            };
            var attendance = AttendanceRecords.IgnoreQueryFilters();
            referenced |= entry.Entity switch
            {
                Company => attendance.Any(r => r.CompanyId == id),
                Shift => attendance.Any(r => r.ShiftId == id),
                WorkLocation => attendance.Any(r => r.WorkLocationId == id),
                _ => false
            };
            if (referenced)
                throw new ConflictException("This organisation record has employee assignments or attendance history and cannot be deleted or moved.");
        }

        var now = _timeProvider.GetUtcNow();
        var userId = _currentUser.UserId;
        foreach (var entry in ChangeTracker.Entries<BaseEntity>().ToArray())
        {
            if (entry.State == EntityState.Added)
            {
                entry.Entity.CreatedAt = now;
                entry.Entity.CreatedBy = userId;
                entry.Entity.UpdatedAt = null;
                entry.Entity.UpdatedBy = null;
                entry.Entity.IsDeleted = false;
            }
            else if (entry.State is EntityState.Modified or EntityState.Deleted)
            {
                if (entry.State == EntityState.Deleted)
                {
                    // Only update deletion/audit columns, including for a detached deletion stub.
                    entry.State = EntityState.Unchanged;
                    entry.Entity.IsDeleted = true;
                    entry.Property(entity => entity.IsDeleted).IsModified = true;
                }

                entry.Property(entity => entity.CreatedAt).IsModified = false;
                entry.Property(entity => entity.CreatedBy).IsModified = false;
                entry.Entity.UpdatedAt = now;
                entry.Entity.UpdatedBy = userId;
                entry.Property(entity => entity.UpdatedAt).IsModified = true;
                entry.Property(entity => entity.UpdatedBy).IsModified = true;
            }
        }
    }
}
