USE [Daas];
GO

IF OBJECT_ID(N'dbo.ApiLinks', N'U') IS NULL
BEGIN
    ;THROW 51000, 'Cannot apply generation-mode update: dbo.ApiLinks does not exist. Apply update 001 first.', 1;
END;
GO

IF NOT EXISTS (SELECT 1 FROM dbo.SchemaMigrations WHERE MigrationId = N'002_add_api_link_generation_mode')
BEGIN
    SET XACT_ABORT ON;

    BEGIN TRY
        BEGIN TRANSACTION;

        IF COL_LENGTH(N'dbo.ApiLinks', N'GenerationMode') IS NULL
        BEGIN
            ALTER TABLE dbo.ApiLinks
                ADD GenerationMode int NOT NULL
                    CONSTRAINT DF_ApiLinks_GenerationMode DEFAULT (0);
        END;

        IF NOT EXISTS
        (
            SELECT 1 FROM sys.check_constraints
            WHERE name = N'CK_ApiLinks_GenerationMode'
              AND parent_object_id = OBJECT_ID(N'dbo.ApiLinks')
        )
        BEGIN
            EXEC(N'ALTER TABLE dbo.ApiLinks ADD CONSTRAINT CK_ApiLinks_GenerationMode CHECK (GenerationMode IN (0, 1));');
        END;

        INSERT INTO dbo.SchemaMigrations (MigrationId) VALUES (N'002_add_api_link_generation_mode');
        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
        THROW;
    END CATCH;
END;
GO
