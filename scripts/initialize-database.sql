IF DB_ID(N'Daas') IS NULL
BEGIN
    CREATE DATABASE [Daas];
END;
GO
USE [Daas];
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
