SET NOCOUNT ON;
SET QUOTED_IDENTIFIER ON;
IF DB_NAME() NOT LIKE 'LCAP_HRMS_Day04[_]%' THROW 51000,'Use an isolated Day04 database.',1;
IF (SELECT COUNT(*) FROM dbo.__EFMigrationsHistory)<>10 THROW 51000,'Expected ten migrations.',1;
IF (SELECT COUNT(*) FROM dbo.AttendancePolicies WHERE PolicyCode='LCAP-STANDARD')<>1 THROW 51000,'Policy seed duplicated.',1;
IF EXISTS(SELECT 1 FROM dbo.WorkLocations WHERE LocationCode='PATNA-OFFICE' AND (Latitude IS NOT NULL OR Longitude IS NOT NULL)) THROW 51000,'Permanent GPS seed changed.',1;
IF EXISTS(SELECT 1 FROM dbo.Companies WHERE CompanyCode='LCAP' AND (PAN IS NOT NULL OR TAN IS NOT NULL OR GSTIN IS NOT NULL OR PFRegistrationNumber IS NOT NULL OR ESIRegistrationNumber IS NOT NULL)) THROW 51000,'Legal seed changed.',1;
IF (SELECT COUNT(*) FROM sys.foreign_keys WHERE parent_object_id IN(OBJECT_ID('dbo.AttendancePolicies'),OBJECT_ID('dbo.AttendanceEvaluations'),OBJECT_ID('dbo.AttendancePenaltyEvents')) AND delete_referential_action=0 AND is_disabled=0 AND is_not_trusted=0)<>10 THROW 51000,'Policy/evaluation/event FKs invalid.',1;
IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID('dbo.AttendancePolicies') AND name='IX_AttendancePolicies_CompanyId_PolicyCode' AND is_unique=1 AND has_filter=0) THROW 51000,'Policy uniqueness missing.',1;
IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID('dbo.AttendanceEvaluations') AND name='IX_AttendanceEvaluations_AttendanceRecordId' AND is_unique=1) THROW 51000,'Evaluation uniqueness missing.',1;
IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID('dbo.AttendancePenaltyEvents') AND name='IX_AttendancePenaltyEvents_AttendanceEvaluationId' AND is_unique=1) THROW 51000,'Event uniqueness missing.',1;
IF EXISTS(SELECT 1 FROM dbo.AttendancePenaltyEvents WHERE Status<>1 OR PayrollConsumedAt IS NOT NULL OR PayrollRunId IS NOT NULL OR CancelledAt IS NOT NULL OR PenaltyValue IS NOT NULL) THROW 51000,'Unexpected event consumption/value.',1;
IF (SELECT COUNT(*) FROM dbo.AttendancePenaltyEvents p JOIN dbo.Employees e ON e.Id=p.EmployeeId WHERE e.EmployeeCode='TEST-DAY04-LATE')<>1 THROW 51000,'LCAP expected exactly one event.',1;
IF (SELECT COUNT(*) FROM dbo.AttendancePenaltyEvents p JOIN dbo.Employees e ON e.Id=p.EmployeeId WHERE e.EmployeeCode='TEST-DAY04-CONCURRENT')<>1 THROW 51000,'Concurrent evaluation duplicated event.',1;
IF NOT EXISTS(SELECT 1 FROM dbo.AttendanceEvaluations v JOIN dbo.Employees e ON e.Id=v.EmployeeId WHERE e.EmployeeCode='TEST-DAY04-LATE' AND v.AttendanceDate='2026-09-10' AND v.ConsecutiveLateCount=3 AND v.ThresholdReached=1 AND v.PenaltyTriggered=0) THROW 51000,'Third late incorrectly triggered event.',1;
IF NOT EXISTS(SELECT 1 FROM dbo.AttendanceEvaluations v JOIN dbo.Employees e ON e.Id=v.EmployeeId WHERE e.EmployeeCode='TEST-DAY04-LATE' AND v.AttendanceDate='2026-09-11' AND v.ConsecutiveLateCount=4 AND v.PenaltyTriggered=1 AND v.SequenceAfterEvaluation=0 AND v.PolicyRevision=1 AND v.GracePeriodMinutes=15) THROW 51000,'Fourth late or immutable snapshot incorrect.',1;
IF NOT EXISTS(SELECT 1 FROM dbo.AttendanceEvaluations v JOIN dbo.Employees e ON e.Id=v.EmployeeId WHERE e.EmployeeCode='TEST-DAY04-LATE' AND v.AttendanceDate='2026-09-12' AND v.ConsecutiveLateCount=1) THROW 51000,'Reset failed.',1;
IF EXISTS(SELECT 1 FROM dbo.AttendanceEvaluations v JOIN dbo.AttendanceRecords r ON r.Id=v.AttendanceRecordId WHERE v.ActualCheckInTime<>r.CheckInTime OR v.CompanyId<>r.CompanyId OR v.EmployeeId<>r.EmployeeId) THROW 51000,'Raw evidence changed or ownership mismatch.',1;
IF EXISTS(SELECT 1 FROM dbo.AttendanceEvaluations WHERE CreatedAt IS NULL OR EvaluatedAt IS NULL OR EvaluationVersion<>1) THROW 51000,'Evaluation audit missing.',1;
DECLARE @evaluation uniqueidentifier=(SELECT TOP(1) Id FROM dbo.AttendanceEvaluations WHERE PenaltyTriggered=1);
BEGIN TRY
 BEGIN TRANSACTION;
 INSERT dbo.AttendancePenaltyEvents(Id,CompanyId,EmployeeId,AttendanceEvaluationId,AttendanceRecordId,AttendancePolicyId,PenaltyDate,PenaltyType,ReasonCode,Status,GeneratedAt,CreatedAt,IsDeleted)
 SELECT NEWID(),CompanyId,EmployeeId,AttendanceEvaluationId,AttendanceRecordId,AttendancePolicyId,PenaltyDate,PenaltyType,ReasonCode,Status,GeneratedAt,CreatedAt,0 FROM dbo.AttendancePenaltyEvents WHERE AttendanceEvaluationId=@evaluation;
 ROLLBACK;THROW 51000,'Duplicate event not blocked.',1;
END TRY BEGIN CATCH IF @@TRANCOUNT>0 ROLLBACK;IF ERROR_NUMBER() NOT IN(2601,2627) THROW;END CATCH;
BEGIN TRY
 BEGIN TRANSACTION;
 DELETE dbo.AttendancePolicies WHERE Id=(SELECT AttendancePolicyId FROM dbo.AttendanceEvaluations WHERE Id=@evaluation);
 ROLLBACK;THROW 51000,'Policy history FK not enforced.',1;
END TRY BEGIN CATCH IF @@TRANCOUNT>0 ROLLBACK;IF ERROR_NUMBER()<>547 THROW;END CATCH;
SELECT CAST(SERVERPROPERTY('ProductVersion') AS nvarchar(30)) AS SqlVersion,(SELECT compatibility_level FROM sys.databases WHERE name=DB_NAME()) AS Compatibility;
SELECT 'PASS: ten migrations, seeds, restrictive FKs, indexes, sequence, idempotency, audit and immutable evidence' AS Result;
