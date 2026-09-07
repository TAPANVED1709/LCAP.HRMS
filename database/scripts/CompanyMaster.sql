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

