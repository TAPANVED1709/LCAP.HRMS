# Backend foundation

## Entity model

Future persisted module entities inherit Domain/Common/BaseEntity. The base class is abstract and is not registered as a standalone table.

| Property | CLR / SQL Server type | Behavior |
| --- | --- | --- |
| Id | Guid / uniqueidentifier | Generated in the entity; primary key, never store-generated |
| CreatedAt | DateTimeOffset / datetimeoffset(7) | UTC creation time; preserved after insert |
| CreatedBy | nullable string / nvarchar(200) | Stable authenticated subject ID, or null |
| UpdatedAt | nullable DateTimeOffset / datetimeoffset(7) | UTC time of update or soft delete |
| UpdatedBy | nullable string / nvarchar(200) | Subject ID of updating actor, or null |
| IsDeleted | bool / bit | False by default; excluded from ordinary queries when true |

EmployeeStatus, EmploymentType, and RecordStatus use explicit numeric values and Unknown = 0. They are placeholders, not lifecycle rules. RecordStatus is separate from soft deletion. No attendance or payroll behavior is included.

## Repositories and committing

IBaseRepository<T> lives in Application. GenericRepository<T> is registered as an open generic in Infrastructure. All repositories in a scope share ApplicationDbContext.

Load an entity with GetByIdAsync, modify its business properties, then call IUnitOfWork.SaveChangesAsync. GetByIdAsync returns a tracked entity, honors the global filter, and rejects a tracked entity marked for deletion. ListAsync returns a no-tracking page ordered by Id, supports an optional predicate, and accepts page sizes from 1 to 1000.

AddAsync and Remove stage changes; they never commit. IUnitOfWork exposes only SaveChangesAsync, which is implemented directly by ApplicationDbContext. One save commits changes from all repositories using EF Core's normal transaction behavior. There is no repository factory, transaction wrapper, or separate UnitOfWork class. Do not run operations concurrently on the same scoped context.

There is deliberately no generic Update method: update entities loaded through GetByIdAsync. Bind request DTOs to explicitly allowed business fields rather than accepting BaseEntity audit/deletion fields from API clients.

## Auditing and soft deletion

Both synchronous and asynchronous SaveChanges overloads use the same audit logic. TimeProvider supplies UTC time, with one timestamp per save. Creation data is generated on insert; subsequent writes preserve it, including detached updates. Updates and deletions stamp UpdatedAt and UpdatedBy. Unchanged entities are not stamped.

The API's CurrentUser reads sub (or NameIdentifier) only from an authenticated principal. Anonymous execution records a null actor. Background hosts should register their own ICurrentUser implementation with a stable service identity or null. Caller-supplied request headers are never used as audit identities.

Calling Remove translates EF's Deleted state into an update of IsDeleted and the update audit fields. Other stored fields are preserved even when deleting a detached stub. Removing a newly added, unsaved entity simply cancels its insertion, following EF Core behavior.

Global query filters are applied automatically to mapped BaseEntity roots. Normal repository reads hide deleted rows. Infrastructure maintenance code can deliberately use IgnoreQueryFilters; the generic repository does not expose that bypass. Soft-delete filters are not authorization or tenant-isolation controls. EF Core 8 supports one filter expression per entity; combine any future tenant/business filter with the soft-delete condition when extending this foundation.

Relationships touching BaseEntity default to Restrict deletion behavior. Deleting a parent does not automatically soft-delete children; future modules must define explicit aggregate deletion rules. Bulk ExecuteDelete, ExecuteUpdate, and raw SQL bypass SaveChanges auditing and must not be used as ordinary audited writes. No purge or restore workflow is implemented yet.

## Configuration and naming

Concrete module mappings should implement IEntityTypeConfiguration<T> in Infrastructure. They are discovered by ApplicationDbContext; BaseEntityConfiguration<T> then applies the common columns and query filter to each mapped root. Register new entity types through their configuration or a DbSet. Do not explicitly register BaseEntity itself.

Use the dbo schema, PascalCase names, and explicit plural table names via ToTable (for example, a future entity configuration can choose its module's table name). Column names follow CLR property names. Retain EF's PK_<Table>, FK_<Dependent>_<Principal>_<Columns>, and IX_<Table>_<Columns> conventions, with SQL Server's 128-character identifier limit. There is no naming plugin or schema-per-module infrastructure yet.

## Migration

InitialFoundation contains empty Up and Down methods. It is the baseline: base classes and enums alone did not require database tables. CompanyMaster and BranchMaster follow with dbo.Companies, dbo.Branches, and their seeds; ApplicationDbContextModelSnapshot reflects the current company/branch model.

Generate reviewable SQL without executing it:

```powershell
dotnet ef migrations script 0 InitialFoundation --idempotent --project backend/LCAP.HRMS.Infrastructure --startup-project backend/LCAP.HRMS.Api --context ApplicationDbContext --output database/scripts/InitialFoundation.sql
```

The InitialFoundation script only establishes EF migration history for that baseline. Use database/scripts/ShiftMaster.sql for the current master-data schema and seeds. No database update or automatic migration is configured.

## Tests

Run dotnet test backend/LCAP.HRMS.sln --configuration Release. The Infrastructure.Tests project uses test-only entities and isolated in-memory SQLite databases to exercise actual persistence behavior. A separate SQL Server model check verifies type mappings and generated filtered SQL without connecting to a server. Test entities are never part of ApplicationDbContext's production model or migrations.
