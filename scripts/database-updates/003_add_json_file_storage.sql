USE [Daas];
GO

SET ANSI_NULLS ON;
SET ANSI_PADDING ON;
SET ANSI_WARNINGS ON;
SET ARITHABORT ON;
SET CONCAT_NULL_YIELDS_NULL ON;
SET QUOTED_IDENTIFIER ON;
SET NUMERIC_ROUNDABORT OFF;
GO

IF OBJECT_ID(N'dbo.Schemas', N'U') IS NULL
    THROW 51000, 'Cannot apply JSON storage update: dbo.Schemas does not exist.', 1;
IF OBJECT_ID(N'dbo.ApiLinks', N'U') IS NULL
    THROW 51000, 'Cannot apply JSON storage update: dbo.ApiLinks does not exist. Apply API-link update 001 first.', 1;
GO

IF OBJECT_ID(N'dbo.SchemaMigrations', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.SchemaMigrations
    (
        MigrationId nvarchar(150) NOT NULL,
        AppliedAtUtc datetime2 NOT NULL CONSTRAINT DF_SchemaMigrations_AppliedAtUtc DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT PK_SchemaMigrations PRIMARY KEY CLUSTERED (MigrationId)
    );
END;
GO

IF NOT EXISTS (SELECT 1 FROM dbo.SchemaMigrations WHERE MigrationId = N'003_add_json_file_storage')
BEGIN
    SET XACT_ABORT ON;
    BEGIN TRY
        BEGIN TRANSACTION;

        IF COL_LENGTH(N'dbo.Schemas', N'SchemaStorageKey') IS NULL
            ALTER TABLE dbo.Schemas ADD SchemaStorageKey uniqueidentifier NULL;
        IF COL_LENGTH(N'dbo.Schemas', N'SchemaVersion') IS NULL
            ALTER TABLE dbo.Schemas ADD SchemaVersion int NOT NULL CONSTRAINT DF_Schemas_SchemaVersion DEFAULT (1);

        IF OBJECT_ID(N'dbo.Datasets', N'U') IS NULL
        BEGIN
            CREATE TABLE dbo.Datasets
            (
                DatasetId uniqueidentifier NOT NULL,
                ApiLinkId uniqueidentifier NOT NULL,
                SchemaId uniqueidentifier NOT NULL,
                SchemaVersion int NOT NULL,
                RecordCount int NOT NULL,
                Version int NOT NULL,
                StorageKey uniqueidentifier NOT NULL,
                IsCurrent bit NOT NULL,
                CreatedAtUtc datetime2 NOT NULL CONSTRAINT DF_Datasets_CreatedAtUtc DEFAULT (SYSUTCDATETIME()),
                ExpiresAtUtc datetime2 NULL,
                CONSTRAINT PK_Datasets PRIMARY KEY CLUSTERED (DatasetId),
                CONSTRAINT FK_Datasets_ApiLinks FOREIGN KEY (ApiLinkId) REFERENCES dbo.ApiLinks(Id) ON DELETE CASCADE,
                CONSTRAINT CK_Datasets_RecordCount CHECK (RecordCount BETWEEN 1 AND 1000),
                CONSTRAINT CK_Datasets_Version CHECK (Version > 0)
            );
            CREATE UNIQUE INDEX UX_Datasets_Current
                ON dbo.Datasets (ApiLinkId, RecordCount) WHERE IsCurrent = 1;
            CREATE UNIQUE INDEX UX_Datasets_Version
                ON dbo.Datasets (ApiLinkId, RecordCount, Version);
            CREATE INDEX IX_Datasets_SchemaId ON dbo.Datasets (SchemaId);
        END;

        IF OBJECT_ID(N'dbo.Datasets', N'U') IS NOT NULL AND COL_LENGTH(N'dbo.Datasets', N'ExpiresAtUtc') IS NULL
            ALTER TABLE dbo.Datasets ADD ExpiresAtUtc datetime2 NULL;

        INSERT INTO dbo.SchemaMigrations (MigrationId) VALUES (N'003_add_json_file_storage');
        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
        THROW;
    END CATCH;
END;
GO
