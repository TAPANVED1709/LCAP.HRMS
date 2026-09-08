SET NOCOUNT ON;
IF DB_NAME() NOT LIKE 'LCAP[_]HRMS[_]Day05[_]%' THROW 51000,'Only isolated Day 5 databases are supported.',1;
IF (SELECT COUNT(*) FROM dbo.__EFMigrationsHistory)<>11 THROW 51000,'Expected eleven migrations.',1;
IF (SELECT COUNT(*) FROM sys.tables WHERE name IN('AttendanceRegularisationRequests','AttendanceCorrections','AttendanceEvaluationRevisions','RegularisationAuditEntries'))<>4 THROW 51000,'Missing Day 5 tables.',1;
IF (SELECT COUNT(*) FROM sys.foreign_keys WHERE parent_object_id IN(OBJECT_ID('dbo.AttendanceRegularisationRequests'),OBJECT_ID('dbo.AttendanceCorrections'),OBJECT_ID('dbo.AttendanceEvaluationRevisions'),OBJECT_ID('dbo.RegularisationAuditEntries')))<>22 THROW 51000,'Expected 22 Day 5 foreign keys.',1;
IF EXISTS(SELECT 1 FROM sys.foreign_keys WHERE parent_object_id IN(OBJECT_ID('dbo.AttendanceRegularisationRequests'),OBJECT_ID('dbo.AttendanceCorrections'),OBJECT_ID('dbo.AttendanceEvaluationRevisions'),OBJECT_ID('dbo.RegularisationAuditEntries')) AND delete_referential_action<>0) THROW 51000,'History FK is not restrictive.',1;
IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID('dbo.AttendanceRegularisationRequests') AND is_unique=1 AND has_filter=1) THROW 51000,'Missing active-request unique index.',1;
IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID('dbo.AttendanceRecords') AND name='IX_AttendanceRecords_EmployeeId_RawOpen' AND is_unique=0) THROW 51000,'Raw-open index must support corrected checkout.',1;
IF (SELECT COUNT(*) FROM dbo.AttendancePolicies WHERE PolicyCode='LCAP-STANDARD')<>1 THROW 51000,'Policy seed duplicated.',1;
IF EXISTS(SELECT 1 FROM dbo.Companies WHERE CompanyCode='LCAP' AND (PAN IS NOT NULL OR TAN IS NOT NULL OR GSTIN IS NOT NULL OR PFRegistrationNumber IS NOT NULL OR ESIRegistrationNumber IS NOT NULL)) THROW 51000,'Fake legal fields inserted.',1;
IF EXISTS(SELECT 1 FROM dbo.WorkLocations WHERE LocationCode NOT LIKE 'TEST-%' AND (Latitude IS NOT NULL OR Longitude IS NOT NULL)) THROW 51000,'Permanent coordinates were changed.',1;
IF EXISTS(SELECT 1 FROM dbo.AttendanceCorrections c JOIN dbo.AttendanceRegularisationRequests r ON r.Id=c.RegularisationRequestId WHERE r.Status<>7 OR r.EmployeeId<>c.EmployeeId OR r.CompanyId<>c.CompanyId) THROW 51000,'Correction ownership/status mismatch.',1;
IF EXISTS(SELECT 1 FROM dbo.AttendanceEvaluationRevisions WHERE ISJSON(ResultJson)<>1 OR Version<1) THROW 51000,'Invalid evaluation snapshot.',1;
IF EXISTS(SELECT 1 FROM dbo.AttendancePenaltyEvents WHERE Status<>1 OR PayrollConsumedAt IS NOT NULL OR PayrollRunId IS NOT NULL OR PenaltyValue IS NOT NULL) THROW 51000,'Penalty was consumed or monetary data introduced.',1;
DECLARE @employee uniqueidentifier=(SELECT Id FROM dbo.Employees WHERE EmployeeCode='TEST-DAY05-REG');
IF NOT EXISTS(SELECT 1 FROM dbo.AttendanceRecords WHERE EmployeeId=@employee AND AttendanceDate='2026-09-11' AND CheckInTime='2026-09-11T04:20:00+00:00' AND CheckInLatitude=0 AND CheckInLongitude=0) THROW 51000,'Raw evidence changed.',1;
IF NOT EXISTS(SELECT 1 FROM dbo.AttendanceCorrections WHERE EmployeeId=@employee AND AttendanceDate='2026-09-11' AND CorrectedCheckInTime='2026-09-11T04:10:00+00:00') THROW 51000,'Missing corrected check-in.',1;
IF NOT EXISTS(SELECT 1 FROM dbo.AttendanceEvaluations WHERE EmployeeId=@employee AND AttendanceDate='2026-09-11' AND IsLate=1 AND PenaltyTriggered=1 AND EvaluationVersion=1) THROW 51000,'Original evaluation changed.',1;
IF NOT EXISTS(SELECT 1 FROM dbo.AttendanceEvaluationRevisions WHERE EmployeeId=@employee AND AttendanceDate='2026-09-11' AND JSON_VALUE(ResultJson,'$.IsLate')='false' AND PolicyImpactReviewRequired=1) THROW 51000,'Reevaluation or review flag missing.',1;
IF (SELECT COUNT(*) FROM dbo.AttendancePenaltyEvents WHERE EmployeeId=@employee AND PenaltyDate='2026-09-11')<>1 THROW 51000,'Original penalty missing or duplicated.',1;
IF EXISTS(SELECT 1 FROM dbo.AttendanceRecords WHERE EmployeeId=@employee AND AttendanceDate='2026-09-13') THROW 51000,'Fabricated raw attendance.',1;
IF NOT EXISTS(SELECT 1 FROM dbo.AttendanceRecords WHERE EmployeeId=@employee AND AttendanceDate='2026-09-14' AND CheckOutTime IS NULL) THROW 51000,'Missed checkout raw was overwritten.',1;
IF NOT EXISTS(SELECT 1 FROM dbo.AttendanceRecords WHERE EmployeeId=@employee AND AttendanceDate='2026-09-20' AND CheckOutTime IS NOT NULL) THROW 51000,'Next check-in after regularisation failed.',1;
IF EXISTS(SELECT RegularisationRequestId FROM dbo.AttendanceCorrections GROUP BY RegularisationRequestId HAVING COUNT(*)>1) THROW 51000,'Duplicate corrections.',1;
IF EXISTS(SELECT RegularisationRequestId FROM dbo.RegularisationAuditEntries WHERE Action IN('Approved','Rejected') GROUP BY RegularisationRequestId HAVING COUNT(*)>1) THROW 51000,'Double review.',1;
IF EXISTS(SELECT 1 FROM dbo.AttendanceRegularisationRequests WHERE Status=7 AND (ReviewedAt IS NULL OR AppliedAt IS NULL OR ReviewedByEmployeeId<>ReportingManagerId)) THROW 51000,'Applied request audit missing.',1;
BEGIN TRY
 BEGIN TRANSACTION;
 INSERT dbo.AttendanceCorrections(Id,CompanyId,EmployeeId,RegularisationRequestId,AttendanceDate,Version,CorrectionType,Reason,EffectiveFrom,AppliedAt,AppliedBy,IsActive,CreatedAt,IsDeleted)
 SELECT TOP(1) NEWID(),CompanyId,EmployeeId,RegularisationRequestId,AttendanceDate,Version+100,CorrectionType,Reason,EffectiveFrom,AppliedAt,AppliedBy,1,CreatedAt,0 FROM dbo.AttendanceCorrections;
 ROLLBACK;THROW 51000,'Duplicate correction not rejected.',1;
END TRY BEGIN CATCH IF @@TRANCOUNT>0 ROLLBACK;IF ERROR_NUMBER() NOT IN(2601,2627) THROW;END CATCH;
BEGIN TRY
 BEGIN TRANSACTION;
 DELETE dbo.AttendanceRegularisationRequests WHERE Id=(SELECT TOP(1) RegularisationRequestId FROM dbo.AttendanceCorrections);
 ROLLBACK;THROW 51000,'Request history FK not enforced.',1;
END TRY BEGIN CATCH IF @@TRANCOUNT>0 ROLLBACK;IF ERROR_NUMBER()<>547 THROW;END CATCH;
SELECT CAST(SERVERPROPERTY('ProductVersion') AS nvarchar(30)) SqlVersion,(SELECT compatibility_level FROM sys.databases WHERE name=DB_NAME()) Compatibility;
SELECT 'PASS: 11 migrations, 22 NO_ACTION FKs, uniqueness, immutable evidence, effective revisions, Pending event review, concurrency and seeds' Result;
