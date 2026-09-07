SET NOCOUNT ON;
IF DB_NAME() NOT LIKE 'LCAP[_]HRMS[_]Day01[_]%' THROW 51100, 'Test database required', 1;
DECLARE @table sysname, @columns nvarchar(max), @values nvarchar(max), @sql nvarchar(max), @error int;
DECLARE tables CURSOR LOCAL FAST_FORWARD FOR SELECT name FROM sys.tables WHERE schema_id=SCHEMA_ID('dbo') AND name IN ('Companies','Branches','Departments','Designations','Shifts','WorkLocations');
OPEN tables;
FETCH NEXT FROM tables INTO @table;
WHILE @@FETCH_STATUS=0
BEGIN
 SELECT @columns=STRING_AGG(CAST(QUOTENAME(name) AS nvarchar(max)),',') WITHIN GROUP (ORDER BY column_id),
 @values=STRING_AGG(CAST(CASE WHEN name='Id' THEN 'NEWID()' ELSE QUOTENAME(name) END AS nvarchar(max)),',') WITHIN GROUP (ORDER BY column_id)
 FROM sys.columns WHERE object_id=OBJECT_ID('dbo.'+@table);
 SET @sql=N'INSERT dbo.'+QUOTENAME(@table)+N'('+@columns+N') SELECT TOP(1) '+@values+N' FROM dbo.'+QUOTENAME(@table)+N' WHERE IsDeleted=0;';
 SET @error=0;
 BEGIN TRANSACTION;
 BEGIN TRY
  EXEC sp_executesql @sql;
 END TRY
 BEGIN CATCH
  SET @error=ERROR_NUMBER();
 END CATCH;
 ROLLBACK TRANSACTION;
 IF @error NOT IN (2601,2627) THROW 51101, 'Expected SQL unique constraint rejection',1;
 PRINT 'PASS duplicate SQL constraint: '+@table;
 SET @sql=N'IF NOT EXISTS(SELECT 1 FROM dbo.'+QUOTENAME(@table)+N' WHERE IsDeleted=1 AND CreatedBy=''day01-integration-tester'' AND UpdatedBy=''day01-integration-tester'') THROW 51102,''Soft-deleted audited row missing'',1;';
 EXEC sp_executesql @sql;
 PRINT 'PASS retained soft-delete/audit row: '+@table;
 FETCH NEXT FROM tables INTO @table;
END
CLOSE tables; DEALLOCATE tables;
SET @error=0;
BEGIN TRANSACTION;
BEGIN TRY
 UPDATE dbo.Shifts SET GracePeriodMinutes=-1 WHERE ShiftCode='GENERAL';
END TRY
BEGIN CATCH
 SET @error=ERROR_NUMBER();
END CATCH;
ROLLBACK;
IF @error<>547 THROW 51103, 'Shift check constraint failed',1;
PRINT 'PASS SQL shift check constraint';
IF EXISTS (SELECT 1 FROM dbo.Companies WHERE PAN IS NOT NULL OR TAN IS NOT NULL OR GSTIN IS NOT NULL OR PFRegistrationNumber IS NOT NULL OR ESIRegistrationNumber IS NOT NULL) THROW 51104,'Unexpected legal identifier',1;
IF EXISTS (SELECT 1 FROM dbo.WorkLocations WHERE Latitude IS NOT NULL OR Longitude IS NOT NULL) THROW 51105,'Unexpected coordinates',1;
SELECT 'PASS all direct SQL checks' AS Result;
