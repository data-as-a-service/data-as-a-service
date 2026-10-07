IF DB_ID(N'Daas') IS NULL
BEGIN
    CREATE DATABASE [Daas];
END;
GO

USE [Daas];
GO

IF OBJECT_ID(N'dbo.Users', N'U') IS NOT NULL
BEGIN
    DROP TABLE [dbo].[Users];
END;
GO

IF OBJECT_ID(N'dbo.Schemas', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[Schemas]
    (
        [Id] uniqueidentifier NOT NULL,
        [Name] nvarchar(max) NOT NULL,
        CONSTRAINT [PK_Schemas] PRIMARY KEY CLUSTERED ([Id])
    );
END;
GO

IF OBJECT_ID(N'dbo.FieldDefinitions', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[FieldDefinitions]
    (
        [Id] int IDENTITY(1,1) NOT NULL,
        [FieldName] nvarchar(max) NOT NULL,
        [FieldType] int NOT NULL,
        [SchemaId] uniqueidentifier NOT NULL,
        CONSTRAINT [PK_FieldDefinitions] PRIMARY KEY CLUSTERED ([Id]),
        CONSTRAINT [FK_FieldDefinitions_Schemas_SchemaId]
            FOREIGN KEY ([SchemaId]) REFERENCES [dbo].[Schemas] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE [name] = N'IX_FieldDefinitions_SchemaId'
      AND [object_id] = OBJECT_ID(N'dbo.FieldDefinitions')
)
BEGIN
    CREATE INDEX [IX_FieldDefinitions_SchemaId]
        ON [dbo].[FieldDefinitions] ([SchemaId]);
END;
GO

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
        CONSTRAINT [CK_ApiLinks_DefaultRecordCount] CHECK ([DefaultRecordCount] BETWEEN 1 AND 1000)
    );
END;
GO

IF NOT EXISTS
(
    SELECT 1 FROM sys.indexes
    WHERE [name] = N'UX_ApiLinks_KeyHash'
      AND [object_id] = OBJECT_ID(N'dbo.ApiLinks')
)
BEGIN
    CREATE UNIQUE INDEX [UX_ApiLinks_KeyHash] ON [dbo].[ApiLinks] ([KeyHash]);
END;
GO

IF NOT EXISTS
(
    SELECT 1 FROM sys.indexes
    WHERE [name] = N'IX_ApiLinks_SchemaId'
      AND [object_id] = OBJECT_ID(N'dbo.ApiLinks')
)
BEGIN
    CREATE INDEX [IX_ApiLinks_SchemaId] ON [dbo].[ApiLinks] ([SchemaId]);
END;
GO
