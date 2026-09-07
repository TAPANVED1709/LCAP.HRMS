SET NOCOUNT ON;
SET QUOTED_IDENTIFIER ON;
IF DB_NAME() NOT LIKE 'LCAP_HRMS_Day03[_]%' THROW 51000, 'Use an isolated Day03 database.', 1;
IF (SELECT COUNT(*) FROM dbo.__EFMigrationsHistory)<>9 THROW 51000, 'Expected nine migrations.', 1;
IF (SELECT COUNT(*) FROM sys.foreign_keys WHERE parent_object_id=OBJECT_ID('dbo.AttendanceRecords') AND is_disabled=0 AND is_not_trusted=0 AND delete_referential_action=0)<>4 THROW 51000, 'Attendance foreign keys invalid.', 1;
IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID('dbo.AttendanceRecords') AND name='UX_AttendanceRecords_EmployeeId_AttendanceDate' AND is_unique=1 AND has_filter=0) THROW 51000, 'Unfiltered attendance uniqueness missing.', 1;
IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID('dbo.AttendanceRecords') AND name='UX_AttendanceRecords_EmployeeId_Open' AND is_unique=1 AND has_filter=1) THROW 51000, 'Open attendance uniqueness missing.', 1;
IF (SELECT COUNT(*) FROM sys.columns WHERE object_id=OBJECT_ID('dbo.AttendanceRecords') AND name IN ('CheckInLatitude','CheckInLongitude','CheckOutLatitude','CheckOutLongitude') AND precision=10 AND scale=7)<>4 THROW 51000, 'GPS precision invalid.', 1;
IF EXISTS(SELECT 1 FROM dbo.Companies WHERE CompanyCode='LCAP' AND (PAN IS NOT NULL OR TAN IS NOT NULL OR GSTIN IS NOT NULL OR PFRegistrationNumber IS NOT NULL OR ESIRegistrationNumber IS NOT NULL)) THROW 51000, 'Legal seeds changed.', 1;
IF EXISTS(SELECT 1 FROM dbo.WorkLocations WHERE LocationCode='PATNA-OFFICE' AND (Latitude IS NOT NULL OR Longitude IS NOT NULL)) THROW 51000, 'Permanent coordinates changed.', 1;
IF (SELECT COUNT(*) FROM dbo.Companies WHERE CompanyCode='LCAP')<>1 THROW 51000, 'Seed duplication.', 1;
IF EXISTS(SELECT 1 FROM dbo.AttendanceRecords WHERE CreatedAt IS NULL OR CreatedBy IS NULL OR (CheckOutTime IS NOT NULL AND (UpdatedBy IS NULL OR UpdatedAt IS NULL))) THROW 51000, 'Audit missing.', 1;
IF EXISTS(SELECT 1 FROM dbo.AttendanceRecords WHERE DATEPART(TZOFFSET,CheckInTime)<>0 OR DATEPART(TZOFFSET,CheckOutTime)<>0) THROW 51000, 'Storage is not UTC.', 1;
IF EXISTS(SELECT 1 FROM dbo.AttendanceRecords r JOIN dbo.Employees e ON r.EmployeeId=e.Id JOIN dbo.WorkLocations w ON r.WorkLocationId=w.Id JOIN dbo.Shifts s ON r.ShiftId=s.Id WHERE r.CompanyId<>e.CompanyId OR r.CompanyId<>w.CompanyId OR r.CompanyId<>s.CompanyId) THROW 51000, 'Attendance ownership mismatch.', 1;
IF NOT EXISTS(SELECT 1 FROM dbo.AttendanceRecords r JOIN dbo.Employees e ON e.Id=r.EmployeeId WHERE e.EmployeeCode='TEST-ATTENDANCE' AND r.CheckInLatitude=0 AND r.CheckOutLongitude=0 AND r.CheckInAccuracyMeters=10 AND r.CheckOutAccuracyMeters=10 AND r.CheckInDistanceMeters=0 AND r.CheckOutDistanceMeters=0 AND r.CheckInWithinGeofence=1 AND r.CheckOutWithinGeofence=1 AND r.TimeZoneId='Asia/Kolkata' AND r.Status=2) THROW 51000, 'Attendance evidence missing.', 1;
DECLARE @employee uniqueidentifier=(SELECT Id FROM dbo.Employees WHERE EmployeeCode='TEST-ATTENDANCE');
BEGIN TRY
 BEGIN TRANSACTION;
 INSERT dbo.AttendanceRecords(Id,CompanyId,EmployeeId,WorkLocationId,ShiftId,AttendanceDate,TimeZoneId,CheckInTime,CheckOutTime,Status,CheckInSource,CreatedAt,IsDeleted)
 SELECT NEWID(),CompanyId,EmployeeId,WorkLocationId,ShiftId,AttendanceDate,TimeZoneId,CheckInTime,CheckOutTime,Status,CheckInSource,CreatedAt,0 FROM dbo.AttendanceRecords WHERE EmployeeId=@employee;
 ROLLBACK;THROW 51000, 'Duplicate constraint failed.', 1;
END TRY
BEGIN CATCH
 IF @@TRANCOUNT>0 ROLLBACK;
 IF ERROR_NUMBER() NOT IN (2601,2627) THROW;
END CATCH;
BEGIN TRY
 BEGIN TRANSACTION;
 UPDATE dbo.AttendanceRecords SET CheckInLatitude=91 WHERE EmployeeId=@employee;
 ROLLBACK;THROW 51000, 'GPS constraint failed.', 1;
END TRY
BEGIN CATCH
 IF @@TRANCOUNT>0 ROLLBACK;
 IF ERROR_NUMBER()<>547 THROW;
END CATCH;
BEGIN TRY
 BEGIN TRANSACTION;
 DELETE dbo.Employees WHERE Id=@employee;
 ROLLBACK;THROW 51000, 'Cascade protection failed.', 1;
END TRY
BEGIN CATCH
 IF @@TRANCOUNT>0 ROLLBACK;
 IF ERROR_NUMBER()<>547 THROW;
END CATCH;
SELECT CAST(SERVERPROPERTY('ProductVersion') AS nvarchar(30)) AS SqlVersion,(SELECT compatibility_level FROM sys.databases WHERE name=DB_NAME()) AS Compatibility;
SELECT 'PASS: migrations, FKs, indexes, precision, seeds, audit, UTC, ownership, evidence and constraints' AS Result;
