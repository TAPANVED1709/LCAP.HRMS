SET NOCOUNT ON;
IF DB_NAME() NOT LIKE 'LCAP_HRMS_Day02[_]%' THROW 51000, 'Use an isolated Day02 database.', 1;
IF (SELECT COUNT(*) FROM dbo.__EFMigrationsHistory) <> 8 THROW 51000, 'Expected eight migrations.', 1;
IF (SELECT COUNT(*) FROM sys.foreign_keys WHERE parent_object_id=OBJECT_ID('dbo.Employees') AND is_disabled=0 AND is_not_trusted=0 AND delete_referential_action=0)<>7 THROW 51000, 'Employee foreign keys invalid.', 1;
IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID('dbo.Employees') AND name='UX_Employees_CompanyId_EmployeeCode' AND is_unique=1) THROW 51000, 'Unique employee code index missing.', 1;
IF NOT EXISTS(SELECT 1 FROM dbo.Employees e JOIN dbo.Employees m ON e.ReportingManagerId=m.Id WHERE e.EmployeeCode='TEST-EMP-001' AND m.EmployeeCode='TEST-MGR-001' AND e.CompanyId=m.CompanyId) THROW 51000, 'Reporting relationship missing.', 1;
IF NOT EXISTS(SELECT 1 FROM dbo.Employees WHERE EmployeeCode='TEST-DELETE' AND IsDeleted=1 AND UpdatedAt IS NOT NULL AND UpdatedBy IS NOT NULL) THROW 51000, 'Soft delete or audit missing.', 1;
IF EXISTS(SELECT 1 FROM dbo.Employees e JOIN dbo.Branches b ON b.Id=e.BranchId JOIN dbo.Departments d ON d.Id=e.DepartmentId JOIN dbo.Designations g ON g.Id=e.DesignationId JOIN dbo.Shifts s ON s.Id=e.ShiftId JOIN dbo.WorkLocations w ON w.Id=e.WorkLocationId WHERE e.CompanyId<>b.CompanyId OR e.CompanyId<>d.CompanyId OR e.CompanyId<>g.CompanyId OR e.CompanyId<>s.CompanyId OR e.CompanyId<>w.CompanyId OR e.BranchId<>w.BranchId) THROW 51000, 'Assignment ownership mismatch.', 1;
IF EXISTS(SELECT 1 FROM dbo.Companies WHERE CompanyCode='LCAP' AND (PAN IS NOT NULL OR TAN IS NOT NULL OR GSTIN IS NOT NULL OR PFRegistrationNumber IS NOT NULL OR ESIRegistrationNumber IS NOT NULL)) THROW 51000, 'Legal seed values changed.', 1;
IF EXISTS(SELECT 1 FROM dbo.WorkLocations WHERE LocationCode='PATNA-OFFICE' AND (Latitude IS NOT NULL OR Longitude IS NOT NULL)) THROW 51000, 'Coordinates changed.', 1;
DECLARE @row uniqueidentifier=(SELECT TOP(1) Id FROM dbo.Employees WHERE EmployeeCode='TEST-DELETE');
BEGIN TRY
 BEGIN TRANSACTION;
 UPDATE dbo.Employees SET ReportingManagerId=Id WHERE Id=@row;
 ROLLBACK;
 THROW 51000, 'Self manager SQL constraint failed.', 1;
END TRY
BEGIN CATCH
 IF @@TRANCOUNT>0 ROLLBACK;
 IF ERROR_NUMBER()<>547 THROW;
END CATCH;
BEGIN TRY
 BEGIN TRANSACTION;
 UPDATE dbo.Employees SET EmployeeCode='TEST-EMP-001' WHERE Id=@row;
 ROLLBACK;
 THROW 51000, 'Unique employee code SQL constraint failed.', 1;
END TRY
BEGIN CATCH
 IF @@TRANCOUNT>0 ROLLBACK;
 IF ERROR_NUMBER() NOT IN (2601,2627) THROW;
END CATCH;
BEGIN TRY
 BEGIN TRANSACTION;
 UPDATE dbo.Employees SET ReportingManagerId=NEWID() WHERE Id=@row;
 ROLLBACK;
 THROW 51000, 'Reporting manager foreign key failed.', 1;
END TRY
BEGIN CATCH
 IF @@TRANCOUNT>0 ROLLBACK;
 IF ERROR_NUMBER()<>547 THROW;
END CATCH;
SELECT 'PASS employee migration, FKs, ownership, reporting relationship, audit, soft delete and seed safety' AS Result;
