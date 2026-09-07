IF OBJECT_ID(N'[__EFMigrationsHistory]') IS NULL
BEGIN
    CREATE TABLE [__EFMigrationsHistory] (
        [MigrationId] nvarchar(150) NOT NULL,
        [ProductVersion] nvarchar(32) NOT NULL,
        CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY ([MigrationId])
    );
END;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260907081957_InitialFoundation'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260907081957_InitialFoundation', N'8.0.30');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260907082739_CompanyMaster'
)
BEGIN
    CREATE TABLE [dbo].[Companies] (
        [Id] uniqueidentifier NOT NULL,
        [CompanyCode] nvarchar(50) COLLATE Latin1_General_100_CI_AS NOT NULL,
        [CompanyName] nvarchar(200) NOT NULL,
        [LegalName] nvarchar(250) NULL,
        [RegisteredAddress] nvarchar(1000) NULL,
        [City] nvarchar(100) NULL,
        [State] nvarchar(100) NULL,
        [Country] nvarchar(100) NOT NULL DEFAULT N'India',
        [PinCode] nvarchar(20) NULL,
        [PAN] nvarchar(50) NULL,
        [TAN] nvarchar(50) NULL,
        [GSTIN] nvarchar(50) NULL,
        [PFRegistrationNumber] nvarchar(50) NULL,
        [ESIRegistrationNumber] nvarchar(50) NULL,
        [PayrollCurrency] nvarchar(3) NOT NULL DEFAULT N'INR',
        [PayrollDay] int NOT NULL,
        [SalaryPaymentDay] int NOT NULL,
        [LogoUrl] nvarchar(2048) NULL,
        [IsActive] bit NOT NULL,
        [CreatedAt] datetimeoffset(7) NOT NULL,
        [CreatedBy] nvarchar(200) NULL,
        [UpdatedAt] datetimeoffset(7) NULL,
        [UpdatedBy] nvarchar(200) NULL,
        [IsDeleted] bit NOT NULL DEFAULT CAST(0 AS bit),
        CONSTRAINT [PK_Companies] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_Companies_CompanyCode] CHECK (LTRIM(RTRIM([CompanyCode])) <> ''),
        CONSTRAINT [CK_Companies_CompanyName] CHECK (LTRIM(RTRIM([CompanyName])) <> ''),
        CONSTRAINT [CK_Companies_PayrollDay] CHECK ([PayrollDay] BETWEEN 1 AND 31),
        CONSTRAINT [CK_Companies_SalaryPaymentDay] CHECK ([SalaryPaymentDay] BETWEEN 1 AND 31)
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260907082739_CompanyMaster'
)
BEGIN
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'City', N'CompanyCode', N'CompanyName', N'Country', N'CreatedAt', N'CreatedBy', N'ESIRegistrationNumber', N'GSTIN', N'IsActive', N'LegalName', N'LogoUrl', N'PAN', N'PFRegistrationNumber', N'PayrollCurrency', N'PayrollDay', N'PinCode', N'RegisteredAddress', N'SalaryPaymentDay', N'State', N'TAN', N'UpdatedAt', N'UpdatedBy') AND [object_id] = OBJECT_ID(N'[dbo].[Companies]'))
        SET IDENTITY_INSERT [dbo].[Companies] ON;
    EXEC(N'INSERT INTO [dbo].[Companies] ([Id], [City], [CompanyCode], [CompanyName], [Country], [CreatedAt], [CreatedBy], [ESIRegistrationNumber], [GSTIN], [IsActive], [LegalName], [LogoUrl], [PAN], [PFRegistrationNumber], [PayrollCurrency], [PayrollDay], [PinCode], [RegisteredAddress], [SalaryPaymentDay], [State], [TAN], [UpdatedAt], [UpdatedBy])
    VALUES (''ec8af472-df65-43d4-9abf-9378e553e455'', NULL, N''LCAP'', N''LCAP'', N''India'', ''2026-09-07T00:00:00.0000000+00:00'', N''system:seed'', NULL, NULL, CAST(1 AS bit), NULL, NULL, NULL, NULL, N''INR'', 1, NULL, NULL, 1, N''Bihar'', NULL, NULL, NULL)');
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'City', N'CompanyCode', N'CompanyName', N'Country', N'CreatedAt', N'CreatedBy', N'ESIRegistrationNumber', N'GSTIN', N'IsActive', N'LegalName', N'LogoUrl', N'PAN', N'PFRegistrationNumber', N'PayrollCurrency', N'PayrollDay', N'PinCode', N'RegisteredAddress', N'SalaryPaymentDay', N'State', N'TAN', N'UpdatedAt', N'UpdatedBy') AND [object_id] = OBJECT_ID(N'[dbo].[Companies]'))
        SET IDENTITY_INSERT [dbo].[Companies] OFF;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260907082739_CompanyMaster'
)
BEGIN
    CREATE UNIQUE INDEX [UX_Companies_CompanyCode] ON [dbo].[Companies] ([CompanyCode]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260907082739_CompanyMaster'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260907082739_CompanyMaster', N'8.0.30');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260907083822_BranchMaster'
)
BEGIN
    CREATE TABLE [dbo].[Branches] (
        [Id] uniqueidentifier NOT NULL,
        [CompanyId] uniqueidentifier NOT NULL,
        [BranchCode] nvarchar(50) COLLATE Latin1_General_100_CI_AS NOT NULL,
        [BranchName] nvarchar(200) NOT NULL,
        [AddressLine1] nvarchar(500) NULL,
        [AddressLine2] nvarchar(500) NULL,
        [City] nvarchar(100) NULL,
        [State] nvarchar(100) NULL,
        [Country] nvarchar(100) NOT NULL DEFAULT N'India',
        [PinCode] nvarchar(20) NULL,
        [Email] nvarchar(254) NULL,
        [Phone] nvarchar(30) NULL,
        [IsHeadOffice] bit NOT NULL,
        [IsActive] bit NOT NULL,
        [CreatedAt] datetimeoffset(7) NOT NULL,
        [CreatedBy] nvarchar(200) NULL,
        [UpdatedAt] datetimeoffset(7) NULL,
        [UpdatedBy] nvarchar(200) NULL,
        [IsDeleted] bit NOT NULL DEFAULT CAST(0 AS bit),
        CONSTRAINT [PK_Branches] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_Branches_BranchCode] CHECK (LTRIM(RTRIM([BranchCode])) <> ''),
        CONSTRAINT [CK_Branches_BranchName] CHECK (LTRIM(RTRIM([BranchName])) <> ''),
        CONSTRAINT [FK_Branches_Companies_CompanyId] FOREIGN KEY ([CompanyId]) REFERENCES [dbo].[Companies] ([Id]) ON DELETE NO ACTION
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260907083822_BranchMaster'
)
BEGIN
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'AddressLine1', N'AddressLine2', N'BranchCode', N'BranchName', N'City', N'CompanyId', N'Country', N'CreatedAt', N'CreatedBy', N'Email', N'IsActive', N'IsHeadOffice', N'Phone', N'PinCode', N'State', N'UpdatedAt', N'UpdatedBy') AND [object_id] = OBJECT_ID(N'[dbo].[Branches]'))
        SET IDENTITY_INSERT [dbo].[Branches] ON;
    EXEC(N'INSERT INTO [dbo].[Branches] ([Id], [AddressLine1], [AddressLine2], [BranchCode], [BranchName], [City], [CompanyId], [Country], [CreatedAt], [CreatedBy], [Email], [IsActive], [IsHeadOffice], [Phone], [PinCode], [State], [UpdatedAt], [UpdatedBy])
    VALUES (''b3e36a15-4efb-441e-bbd8-f6cf916f6107'', NULL, NULL, N''PATNA-HO'', N''Patna Head Office'', N''Patna'', ''ec8af472-df65-43d4-9abf-9378e553e455'', N''India'', ''2026-09-07T00:00:00.0000000+00:00'', N''system:seed'', NULL, CAST(1 AS bit), CAST(1 AS bit), NULL, NULL, N''Bihar'', NULL, NULL)');
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'AddressLine1', N'AddressLine2', N'BranchCode', N'BranchName', N'City', N'CompanyId', N'Country', N'CreatedAt', N'CreatedBy', N'Email', N'IsActive', N'IsHeadOffice', N'Phone', N'PinCode', N'State', N'UpdatedAt', N'UpdatedBy') AND [object_id] = OBJECT_ID(N'[dbo].[Branches]'))
        SET IDENTITY_INSERT [dbo].[Branches] OFF;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260907083822_BranchMaster'
)
BEGIN
    CREATE UNIQUE INDEX [UX_Branches_CompanyId_BranchCode] ON [dbo].[Branches] ([CompanyId], [BranchCode]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260907083822_BranchMaster'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260907083822_BranchMaster', N'8.0.30');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260907095757_DepartmentMaster'
)
BEGIN
    CREATE TABLE [dbo].[Departments] (
        [Id] uniqueidentifier NOT NULL,
        [CompanyId] uniqueidentifier NOT NULL,
        [DepartmentCode] nvarchar(50) COLLATE Latin1_General_100_CI_AS NOT NULL,
        [DepartmentName] nvarchar(200) NOT NULL,
        [Description] nvarchar(1000) NULL,
        [ParentDepartmentId] uniqueidentifier NULL,
        [IsActive] bit NOT NULL,
        [CreatedAt] datetimeoffset(7) NOT NULL,
        [CreatedBy] nvarchar(200) NULL,
        [UpdatedAt] datetimeoffset(7) NULL,
        [UpdatedBy] nvarchar(200) NULL,
        [IsDeleted] bit NOT NULL DEFAULT CAST(0 AS bit),
        CONSTRAINT [PK_Departments] PRIMARY KEY ([Id]),
        CONSTRAINT [AK_Departments_CompanyId_Id] UNIQUE ([CompanyId], [Id]),
        CONSTRAINT [CK_Departments_DepartmentCode] CHECK (LTRIM(RTRIM([DepartmentCode])) <> ''),
        CONSTRAINT [CK_Departments_DepartmentName] CHECK (LTRIM(RTRIM([DepartmentName])) <> ''),
        CONSTRAINT [CK_Departments_NotOwnParent] CHECK ([ParentDepartmentId] IS NULL OR [ParentDepartmentId] <> [Id]),
        CONSTRAINT [FK_Departments_Companies_CompanyId] FOREIGN KEY ([CompanyId]) REFERENCES [dbo].[Companies] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Departments_Departments_CompanyId_ParentDepartmentId] FOREIGN KEY ([CompanyId], [ParentDepartmentId]) REFERENCES [dbo].[Departments] ([CompanyId], [Id]) ON DELETE NO ACTION
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260907095757_DepartmentMaster'
)
BEGIN
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'CompanyId', N'CreatedAt', N'CreatedBy', N'DepartmentCode', N'DepartmentName', N'Description', N'IsActive', N'ParentDepartmentId', N'UpdatedAt', N'UpdatedBy') AND [object_id] = OBJECT_ID(N'[dbo].[Departments]'))
        SET IDENTITY_INSERT [dbo].[Departments] ON;
    EXEC(N'INSERT INTO [dbo].[Departments] ([Id], [CompanyId], [CreatedAt], [CreatedBy], [DepartmentCode], [DepartmentName], [Description], [IsActive], [ParentDepartmentId], [UpdatedAt], [UpdatedBy])
    VALUES (''0ebde002-3e2e-4ac4-bcca-7a0ea3bd4201'', ''ec8af472-df65-43d4-9abf-9378e553e455'', ''2026-09-07T00:00:00.0000000+00:00'', N''system:seed'', N''HR'', N''Human Resources'', NULL, CAST(1 AS bit), NULL, NULL, NULL),
    (''0ebde002-3e2e-4ac4-bcca-7a0ea3bd4202'', ''ec8af472-df65-43d4-9abf-9378e553e455'', ''2026-09-07T00:00:00.0000000+00:00'', N''system:seed'', N''FIN'', N''Finance'', NULL, CAST(1 AS bit), NULL, NULL, NULL),
    (''0ebde002-3e2e-4ac4-bcca-7a0ea3bd4203'', ''ec8af472-df65-43d4-9abf-9378e553e455'', ''2026-09-07T00:00:00.0000000+00:00'', N''system:seed'', N''SALES'', N''Sales'', NULL, CAST(1 AS bit), NULL, NULL, NULL),
    (''0ebde002-3e2e-4ac4-bcca-7a0ea3bd4204'', ''ec8af472-df65-43d4-9abf-9378e553e455'', ''2026-09-07T00:00:00.0000000+00:00'', N''system:seed'', N''OPS'', N''Operations'', NULL, CAST(1 AS bit), NULL, NULL, NULL),
    (''0ebde002-3e2e-4ac4-bcca-7a0ea3bd4205'', ''ec8af472-df65-43d4-9abf-9378e553e455'', ''2026-09-07T00:00:00.0000000+00:00'', N''system:seed'', N''MGMT'', N''Management'', NULL, CAST(1 AS bit), NULL, NULL, NULL)');
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'CompanyId', N'CreatedAt', N'CreatedBy', N'DepartmentCode', N'DepartmentName', N'Description', N'IsActive', N'ParentDepartmentId', N'UpdatedAt', N'UpdatedBy') AND [object_id] = OBJECT_ID(N'[dbo].[Departments]'))
        SET IDENTITY_INSERT [dbo].[Departments] OFF;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260907095757_DepartmentMaster'
)
BEGIN
    CREATE INDEX [IX_Departments_CompanyId_ParentDepartmentId] ON [dbo].[Departments] ([CompanyId], [ParentDepartmentId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260907095757_DepartmentMaster'
)
BEGIN
    CREATE INDEX [IX_Departments_ParentDepartmentId] ON [dbo].[Departments] ([ParentDepartmentId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260907095757_DepartmentMaster'
)
BEGIN
    CREATE UNIQUE INDEX [UX_Departments_CompanyId_DepartmentCode] ON [dbo].[Departments] ([CompanyId], [DepartmentCode]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260907095757_DepartmentMaster'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260907095757_DepartmentMaster', N'8.0.30');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260907100522_DesignationMaster'
)
BEGIN
    CREATE TABLE [dbo].[Designations] (
        [Id] uniqueidentifier NOT NULL,
        [CompanyId] uniqueidentifier NOT NULL,
        [DesignationCode] nvarchar(50) COLLATE Latin1_General_100_CI_AS NOT NULL,
        [DesignationName] nvarchar(200) NOT NULL,
        [Description] nvarchar(1000) NULL,
        [Grade] nvarchar(50) NULL,
        [Level] int NULL,
        [IsManagerial] bit NOT NULL DEFAULT CAST(0 AS bit),
        [IsActive] bit NOT NULL,
        [CreatedAt] datetimeoffset(7) NOT NULL,
        [CreatedBy] nvarchar(200) NULL,
        [UpdatedAt] datetimeoffset(7) NULL,
        [UpdatedBy] nvarchar(200) NULL,
        [IsDeleted] bit NOT NULL DEFAULT CAST(0 AS bit),
        CONSTRAINT [PK_Designations] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_Designations_DesignationCode] CHECK (LTRIM(RTRIM([DesignationCode])) <> ''),
        CONSTRAINT [CK_Designations_DesignationName] CHECK (LTRIM(RTRIM([DesignationName])) <> ''),
        CONSTRAINT [FK_Designations_Companies_CompanyId] FOREIGN KEY ([CompanyId]) REFERENCES [dbo].[Companies] ([Id]) ON DELETE NO ACTION
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260907100522_DesignationMaster'
)
BEGIN
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'CompanyId', N'CreatedAt', N'CreatedBy', N'Description', N'DesignationCode', N'DesignationName', N'Grade', N'IsActive', N'Level', N'UpdatedAt', N'UpdatedBy') AND [object_id] = OBJECT_ID(N'[dbo].[Designations]'))
        SET IDENTITY_INSERT [dbo].[Designations] ON;
    EXEC(N'INSERT INTO [dbo].[Designations] ([Id], [CompanyId], [CreatedAt], [CreatedBy], [Description], [DesignationCode], [DesignationName], [Grade], [IsActive], [Level], [UpdatedAt], [UpdatedBy])
    VALUES (''42d49a28-e6b8-46c7-aac1-11019a39d001'', ''ec8af472-df65-43d4-9abf-9378e553e455'', ''2026-09-07T00:00:00.0000000+00:00'', N''system:seed'', NULL, N''EXEC'', N''Executive'', NULL, CAST(1 AS bit), NULL, NULL, NULL),
    (''42d49a28-e6b8-46c7-aac1-11019a39d002'', ''ec8af472-df65-43d4-9abf-9378e553e455'', ''2026-09-07T00:00:00.0000000+00:00'', N''system:seed'', NULL, N''SREXEC'', N''Senior Executive'', NULL, CAST(1 AS bit), NULL, NULL, NULL),
    (''42d49a28-e6b8-46c7-aac1-11019a39d003'', ''ec8af472-df65-43d4-9abf-9378e553e455'', ''2026-09-07T00:00:00.0000000+00:00'', N''system:seed'', NULL, N''TL'', N''Team Leader'', NULL, CAST(1 AS bit), NULL, NULL, NULL),
    (''42d49a28-e6b8-46c7-aac1-11019a39d004'', ''ec8af472-df65-43d4-9abf-9378e553e455'', ''2026-09-07T00:00:00.0000000+00:00'', N''system:seed'', NULL, N''MGR'', N''Manager'', NULL, CAST(1 AS bit), NULL, NULL, NULL),
    (''42d49a28-e6b8-46c7-aac1-11019a39d005'', ''ec8af472-df65-43d4-9abf-9378e553e455'', ''2026-09-07T00:00:00.0000000+00:00'', N''system:seed'', NULL, N''HRM'', N''HR Manager'', NULL, CAST(1 AS bit), NULL, NULL, NULL),
    (''42d49a28-e6b8-46c7-aac1-11019a39d006'', ''ec8af472-df65-43d4-9abf-9378e553e455'', ''2026-09-07T00:00:00.0000000+00:00'', N''system:seed'', NULL, N''PAYADMIN'', N''Payroll Admin'', NULL, CAST(1 AS bit), NULL, NULL, NULL)');
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'CompanyId', N'CreatedAt', N'CreatedBy', N'Description', N'DesignationCode', N'DesignationName', N'Grade', N'IsActive', N'Level', N'UpdatedAt', N'UpdatedBy') AND [object_id] = OBJECT_ID(N'[dbo].[Designations]'))
        SET IDENTITY_INSERT [dbo].[Designations] OFF;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260907100522_DesignationMaster'
)
BEGIN
    CREATE UNIQUE INDEX [UX_Designations_CompanyId_DesignationCode] ON [dbo].[Designations] ([CompanyId], [DesignationCode]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260907100522_DesignationMaster'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260907100522_DesignationMaster', N'8.0.30');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260907101327_WorkLocationMaster'
)
BEGIN
    CREATE TABLE [dbo].[WorkLocations] (
        [Id] uniqueidentifier NOT NULL,
        [CompanyId] uniqueidentifier NOT NULL,
        [BranchId] uniqueidentifier NOT NULL,
        [LocationCode] nvarchar(50) COLLATE Latin1_General_100_CI_AS NOT NULL,
        [LocationName] nvarchar(200) NOT NULL,
        [Address] nvarchar(1000) NULL,
        [City] nvarchar(100) NULL,
        [State] nvarchar(100) NULL,
        [Country] nvarchar(100) NOT NULL DEFAULT N'India',
        [PinCode] nvarchar(20) NULL,
        [Latitude] decimal(10,7) NULL,
        [Longitude] decimal(10,7) NULL,
        [AllowedRadiusMeters] int NOT NULL DEFAULT 100,
        [IsGeoFenceEnabled] bit NOT NULL DEFAULT CAST(1 AS bit),
        [IsActive] bit NOT NULL,
        [CreatedAt] datetimeoffset(7) NOT NULL,
        [CreatedBy] nvarchar(200) NULL,
        [UpdatedAt] datetimeoffset(7) NULL,
        [UpdatedBy] nvarchar(200) NULL,
        [IsDeleted] bit NOT NULL DEFAULT CAST(0 AS bit),
        CONSTRAINT [PK_WorkLocations] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_WorkLocations_CoordinatePair] CHECK (([Latitude] IS NULL AND [Longitude] IS NULL) OR ([Latitude] IS NOT NULL AND [Longitude] IS NOT NULL)),
        CONSTRAINT [CK_WorkLocations_Latitude] CHECK ([Latitude] IS NULL OR [Latitude] BETWEEN -90 AND 90),
        CONSTRAINT [CK_WorkLocations_LocationCode] CHECK (LTRIM(RTRIM([LocationCode])) <> ''),
        CONSTRAINT [CK_WorkLocations_LocationName] CHECK (LTRIM(RTRIM([LocationName])) <> ''),
        CONSTRAINT [CK_WorkLocations_Longitude] CHECK ([Longitude] IS NULL OR [Longitude] BETWEEN -180 AND 180),
        CONSTRAINT [CK_WorkLocations_Radius] CHECK ([AllowedRadiusMeters] > 0),
        CONSTRAINT [FK_WorkLocations_Branches_BranchId] FOREIGN KEY ([BranchId]) REFERENCES [dbo].[Branches] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_WorkLocations_Companies_CompanyId] FOREIGN KEY ([CompanyId]) REFERENCES [dbo].[Companies] ([Id]) ON DELETE NO ACTION
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260907101327_WorkLocationMaster'
)
BEGIN
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'Address', N'AllowedRadiusMeters', N'BranchId', N'City', N'CompanyId', N'Country', N'CreatedAt', N'CreatedBy', N'IsActive', N'IsGeoFenceEnabled', N'Latitude', N'LocationCode', N'LocationName', N'Longitude', N'PinCode', N'State', N'UpdatedAt', N'UpdatedBy') AND [object_id] = OBJECT_ID(N'[dbo].[WorkLocations]'))
        SET IDENTITY_INSERT [dbo].[WorkLocations] ON;
    EXEC(N'INSERT INTO [dbo].[WorkLocations] ([Id], [Address], [AllowedRadiusMeters], [BranchId], [City], [CompanyId], [Country], [CreatedAt], [CreatedBy], [IsActive], [IsGeoFenceEnabled], [Latitude], [LocationCode], [LocationName], [Longitude], [PinCode], [State], [UpdatedAt], [UpdatedBy])
    VALUES (''638b4a96-b60f-4cfb-b5a8-5bf4b304896a'', NULL, 100, ''b3e36a15-4efb-441e-bbd8-f6cf916f6107'', N''Patna'', ''ec8af472-df65-43d4-9abf-9378e553e455'', N''India'', ''2026-09-07T00:00:00.0000000+00:00'', N''system:seed'', CAST(1 AS bit), CAST(1 AS bit), NULL, N''PATNA-OFFICE'', N''Patna Office'', NULL, NULL, N''Bihar'', NULL, NULL)');
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'Address', N'AllowedRadiusMeters', N'BranchId', N'City', N'CompanyId', N'Country', N'CreatedAt', N'CreatedBy', N'IsActive', N'IsGeoFenceEnabled', N'Latitude', N'LocationCode', N'LocationName', N'Longitude', N'PinCode', N'State', N'UpdatedAt', N'UpdatedBy') AND [object_id] = OBJECT_ID(N'[dbo].[WorkLocations]'))
        SET IDENTITY_INSERT [dbo].[WorkLocations] OFF;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260907101327_WorkLocationMaster'
)
BEGIN
    CREATE INDEX [IX_WorkLocations_CompanyId] ON [dbo].[WorkLocations] ([CompanyId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260907101327_WorkLocationMaster'
)
BEGIN
    CREATE UNIQUE INDEX [UX_WorkLocations_BranchId_LocationCode] ON [dbo].[WorkLocations] ([BranchId], [LocationCode]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260907101327_WorkLocationMaster'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260907101327_WorkLocationMaster', N'8.0.30');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260907102112_ShiftMaster'
)
BEGIN
    CREATE TABLE [dbo].[Shifts] (
        [Id] uniqueidentifier NOT NULL,
        [CompanyId] uniqueidentifier NOT NULL,
        [ShiftCode] nvarchar(50) COLLATE Latin1_General_100_CI_AS NOT NULL,
        [ShiftName] nvarchar(200) NOT NULL,
        [StartTime] time(7) NOT NULL,
        [EndTime] time(7) NOT NULL,
        [GracePeriodMinutes] int NOT NULL,
        [MinimumHalfDayMinutes] int NULL,
        [MinimumFullDayMinutes] int NULL,
        [IsNightShift] bit NOT NULL,
        [IsActive] bit NOT NULL,
        [CreatedAt] datetimeoffset(7) NOT NULL,
        [CreatedBy] nvarchar(200) NULL,
        [UpdatedAt] datetimeoffset(7) NULL,
        [UpdatedBy] nvarchar(200) NULL,
        [IsDeleted] bit NOT NULL DEFAULT CAST(0 AS bit),
        CONSTRAINT [PK_Shifts] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_Shifts_FullDay] CHECK ([MinimumFullDayMinutes] IS NULL OR [MinimumFullDayMinutes] >= 0),
        CONSTRAINT [CK_Shifts_GracePeriod] CHECK ([GracePeriodMinutes] >= 0),
        CONSTRAINT [CK_Shifts_HalfDay] CHECK ([MinimumHalfDayMinutes] IS NULL OR [MinimumHalfDayMinutes] >= 0),
        CONSTRAINT [CK_Shifts_NightShift] CHECK ([EndTime] > [StartTime] OR [IsNightShift] = 1),
        CONSTRAINT [CK_Shifts_ShiftCode] CHECK (LTRIM(RTRIM([ShiftCode])) <> ''),
        CONSTRAINT [CK_Shifts_ShiftName] CHECK (LTRIM(RTRIM([ShiftName])) <> ''),
        CONSTRAINT [CK_Shifts_ThresholdOrder] CHECK ([MinimumHalfDayMinutes] IS NULL OR [MinimumFullDayMinutes] IS NULL OR [MinimumHalfDayMinutes] <= [MinimumFullDayMinutes]),
        CONSTRAINT [CK_Shifts_Times] CHECK ([StartTime] <> [EndTime]),
        CONSTRAINT [FK_Shifts_Companies_CompanyId] FOREIGN KEY ([CompanyId]) REFERENCES [dbo].[Companies] ([Id]) ON DELETE NO ACTION
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260907102112_ShiftMaster'
)
BEGIN
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'CompanyId', N'CreatedAt', N'CreatedBy', N'EndTime', N'GracePeriodMinutes', N'IsActive', N'IsNightShift', N'MinimumFullDayMinutes', N'MinimumHalfDayMinutes', N'ShiftCode', N'ShiftName', N'StartTime', N'UpdatedAt', N'UpdatedBy') AND [object_id] = OBJECT_ID(N'[dbo].[Shifts]'))
        SET IDENTITY_INSERT [dbo].[Shifts] ON;
    EXEC(N'INSERT INTO [dbo].[Shifts] ([Id], [CompanyId], [CreatedAt], [CreatedBy], [EndTime], [GracePeriodMinutes], [IsActive], [IsNightShift], [MinimumFullDayMinutes], [MinimumHalfDayMinutes], [ShiftCode], [ShiftName], [StartTime], [UpdatedAt], [UpdatedBy])
    VALUES (''a0c85f4b-a175-441a-973e-9f66d6ed0001'', ''ec8af472-df65-43d4-9abf-9378e553e455'', ''2026-09-07T00:00:00.0000000+00:00'', N''system:seed'', ''18:30:00'', 15, CAST(1 AS bit), CAST(0 AS bit), NULL, NULL, N''GENERAL'', N''General Shift'', ''09:30:00'', NULL, NULL)');
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'CompanyId', N'CreatedAt', N'CreatedBy', N'EndTime', N'GracePeriodMinutes', N'IsActive', N'IsNightShift', N'MinimumFullDayMinutes', N'MinimumHalfDayMinutes', N'ShiftCode', N'ShiftName', N'StartTime', N'UpdatedAt', N'UpdatedBy') AND [object_id] = OBJECT_ID(N'[dbo].[Shifts]'))
        SET IDENTITY_INSERT [dbo].[Shifts] OFF;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260907102112_ShiftMaster'
)
BEGIN
    CREATE UNIQUE INDEX [UX_Shifts_CompanyId_ShiftCode] ON [dbo].[Shifts] ([CompanyId], [ShiftCode]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260907102112_ShiftMaster'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260907102112_ShiftMaster', N'8.0.30');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260907131530_EmployeeMaster'
)
BEGIN
    CREATE TABLE [dbo].[Employees] (
        [Id] uniqueidentifier NOT NULL,
        [CompanyId] uniqueidentifier NOT NULL,
        [BranchId] uniqueidentifier NOT NULL,
        [DepartmentId] uniqueidentifier NOT NULL,
        [DesignationId] uniqueidentifier NOT NULL,
        [ShiftId] uniqueidentifier NOT NULL,
        [WorkLocationId] uniqueidentifier NOT NULL,
        [ReportingManagerId] uniqueidentifier NULL,
        [EmployeeCode] nvarchar(50) COLLATE Latin1_General_100_CI_AS NOT NULL,
        [FirstName] nvarchar(100) NOT NULL,
        [MiddleName] nvarchar(100) NULL,
        [LastName] nvarchar(100) NULL,
        [MobileNumber] nvarchar(25) NOT NULL,
        [AlternateMobileNumber] nvarchar(25) NULL,
        [PersonalEmail] nvarchar(254) NULL,
        [OfficialEmail] nvarchar(254) NULL,
        [DateOfBirth] date NULL,
        [Gender] nvarchar(50) NULL,
        [BloodGroup] nvarchar(10) NULL,
        [DateOfJoining] date NOT NULL,
        [DateOfConfirmation] date NULL,
        [EmploymentType] int NOT NULL,
        [EmployeeStatus] int NOT NULL,
        [ProbationEndDate] date NULL,
        [DateOfResignation] date NULL,
        [LastWorkingDate] date NULL,
        [ExitReason] nvarchar(1000) NULL,
        [PAN] nvarchar(10) NULL,
        [AadhaarNumber] nvarchar(12) NULL,
        [UAN] nvarchar(20) NULL,
        [ESICNumber] nvarchar(20) NULL,
        [BankName] nvarchar(200) NULL,
        [AccountHolderName] nvarchar(200) NULL,
        [BankAccountNumber] nvarchar(34) NULL,
        [IFSCCode] nvarchar(11) NULL,
        [ProfilePhotoUrl] nvarchar(2048) NULL,
        [Notes] nvarchar(2000) NULL,
        [IsActive] bit NOT NULL,
        [CreatedAt] datetimeoffset(7) NOT NULL,
        [CreatedBy] nvarchar(200) NULL,
        [UpdatedAt] datetimeoffset(7) NULL,
        [UpdatedBy] nvarchar(200) NULL,
        [IsDeleted] bit NOT NULL DEFAULT CAST(0 AS bit),
        CONSTRAINT [PK_Employees] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_Employees_Code] CHECK (LTRIM(RTRIM([EmployeeCode])) <> ''),
        CONSTRAINT [CK_Employees_Confirmation] CHECK ([DateOfConfirmation] IS NULL OR [DateOfConfirmation] >= [DateOfJoining]),
        CONSTRAINT [CK_Employees_LastWorking] CHECK ([LastWorkingDate] IS NULL OR [LastWorkingDate] >= [DateOfJoining]),
        CONSTRAINT [CK_Employees_SelfManager] CHECK ([ReportingManagerId] IS NULL OR [ReportingManagerId] <> [Id]),
        CONSTRAINT [FK_Employees_Branches_BranchId] FOREIGN KEY ([BranchId]) REFERENCES [dbo].[Branches] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Employees_Companies_CompanyId] FOREIGN KEY ([CompanyId]) REFERENCES [dbo].[Companies] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Employees_Departments_DepartmentId] FOREIGN KEY ([DepartmentId]) REFERENCES [dbo].[Departments] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Employees_Designations_DesignationId] FOREIGN KEY ([DesignationId]) REFERENCES [dbo].[Designations] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Employees_Employees_ReportingManagerId] FOREIGN KEY ([ReportingManagerId]) REFERENCES [dbo].[Employees] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Employees_Shifts_ShiftId] FOREIGN KEY ([ShiftId]) REFERENCES [dbo].[Shifts] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Employees_WorkLocations_WorkLocationId] FOREIGN KEY ([WorkLocationId]) REFERENCES [dbo].[WorkLocations] ([Id]) ON DELETE NO ACTION
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260907131530_EmployeeMaster'
)
BEGIN
    CREATE INDEX [IX_Employees_BranchId] ON [dbo].[Employees] ([BranchId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260907131530_EmployeeMaster'
)
BEGIN
    CREATE INDEX [IX_Employees_CompanyId_IsDeleted] ON [dbo].[Employees] ([CompanyId], [IsDeleted]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260907131530_EmployeeMaster'
)
BEGIN
    CREATE INDEX [IX_Employees_DepartmentId] ON [dbo].[Employees] ([DepartmentId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260907131530_EmployeeMaster'
)
BEGIN
    CREATE INDEX [IX_Employees_DesignationId] ON [dbo].[Employees] ([DesignationId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260907131530_EmployeeMaster'
)
BEGIN
    CREATE INDEX [IX_Employees_EmployeeStatus] ON [dbo].[Employees] ([EmployeeStatus]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260907131530_EmployeeMaster'
)
BEGIN
    CREATE INDEX [IX_Employees_ReportingManagerId] ON [dbo].[Employees] ([ReportingManagerId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260907131530_EmployeeMaster'
)
BEGIN
    CREATE INDEX [IX_Employees_ShiftId] ON [dbo].[Employees] ([ShiftId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260907131530_EmployeeMaster'
)
BEGIN
    CREATE INDEX [IX_Employees_WorkLocationId] ON [dbo].[Employees] ([WorkLocationId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260907131530_EmployeeMaster'
)
BEGIN
    CREATE UNIQUE INDEX [UX_Employees_CompanyId_EmployeeCode] ON [dbo].[Employees] ([CompanyId], [EmployeeCode]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260907131530_EmployeeMaster'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260907131530_EmployeeMaster', N'8.0.30');
END;
GO

COMMIT;
GO

