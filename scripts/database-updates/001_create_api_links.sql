USE [Daas];
GO

IF OBJECT_ID(N'dbo.Schemas', N'U') IS NULL
BEGIN
    ;THROW 51000, 'Cannot apply API-link update: dbo.Schemas does not exist. Run initialize-database.sql first.', 1;
END;
GO

IF OBJECT_ID(N'dbo.SchemaMigrations', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[SchemaMigrations]
    (
        [MigrationId] nvarchar(150) NOT NULL,
        [AppliedAtUtc] datetime2 NOT NULL CONSTRAINT [DF_SchemaMigrations_AppliedAtUtc] DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT [PK_SchemaMigrations] PRIMARY KEY CLUSTERED ([MigrationId])
    );
END;
GO

IF NOT EXISTS (SELECT 1 FROM dbo.SchemaMigrations WHERE MigrationId = N'001_create_api_links')
BEGIN
    SET XACT_ABORT ON;

    BEGIN TRY
        BEGIN TRANSACTION;

        IF OBJECT_ID(N'dbo.ApiLinks', N'U') IS NULL
        BEGIN
            CREATE TABLE [dbo].[ApiLinks]
            (
                [Id] uniqueidentifier NOT NULL,
                [SchemaId] uniqueidentifier NOT NULL,
                [KeyHash] char(64) NOT NULL,
                [IsActive] bit NOT NULL CONSTRAINT [DF_ApiLinks_IsActive] DEFAULT (1),
                [CreatedAt] datetime2 NOT NULL CONSTRAINT [DF_ApiLinks_CreatedAt] DEFAULT (SYSUTCDATETIME()),
                [ExpiresAt] datetime2 NULL,
                [DefaultRecordCount] int NOT NULL,
                CONSTRAINT [PK_ApiLinks] PRIMARY KEY CLUSTERED ([Id]),
                CONSTRAINT [FK_ApiLinks_Schemas_SchemaId]
                    FOREIGN KEY ([SchemaId]) REFERENCES [dbo].[Schemas] ([Id]) ON DELETE CASCADE,
                CONSTRAINT [CK_ApiLinks_DefaultRecordCount]
                    CHECK ([DefaultRecordCount] BETWEEN 1 AND 1000)
            );
        END;

        IF NOT EXISTS
        (
            SELECT 1 FROM sys.indexes
            WHERE [name] = N'UX_ApiLinks_KeyHash'
              AND [object_id] = OBJECT_ID(N'dbo.ApiLinks')
        )
        BEGIN
            CREATE UNIQUE INDEX [UX_ApiLinks_KeyHash] ON [dbo].[ApiLinks] ([KeyHash]);
        END;

        IF NOT EXISTS
        (
            SELECT 1 FROM sys.indexes
            WHERE [name] = N'IX_ApiLinks_SchemaId'
              AND [object_id] = OBJECT_ID(N'dbo.ApiLinks')
        )
        BEGIN
            CREATE INDEX [IX_ApiLinks_SchemaId] ON [dbo].[ApiLinks] ([SchemaId]);
        END;

        INSERT INTO dbo.SchemaMigrations (MigrationId) VALUES (N'001_create_api_links');
        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
        THROW;
    END CATCH;
END;
GO
