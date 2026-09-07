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

