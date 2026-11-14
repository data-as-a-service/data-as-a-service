/*
    Diagnose schemas whose SQL metadata points to missing JSON files.

    JSON file paths are composed by the API, so SQL Server cannot test whether a
    file exists on the API host. This script classifies schemas for recovery:

      - RECOVERABLE_FROM_LEGACY_FIELDS: clearing SchemaStorageKey allows the API
        to reconstruct the JSON document from dbo.FieldDefinitions on next read.
      - FILE_BACKUP_REQUIRED: legacy field rows are absent; restore the JSON file
        from a matching backup. Clearing the key would produce an empty schema.

    Review output and take a database backup before running any manual repair.
    This diagnostic script does not modify data.
*/
USE [Daas];
GO

IF OBJECT_ID(N'dbo.Schemas', N'U') IS NULL
    THROW 51000, 'dbo.Schemas does not exist in Daas.', 1;
IF COL_LENGTH(N'dbo.Schemas', N'SchemaStorageKey') IS NULL
    THROW 51000, 'SchemaStorageKey does not exist. Apply database update 003 first.', 1;
IF OBJECT_ID(N'dbo.FieldDefinitions', N'U') IS NULL
    THROW 51000, 'dbo.FieldDefinitions does not exist.', 1;
GO

SELECT
    s.Id AS SchemaId,
    s.Name,
    s.SchemaStorageKey,
    COUNT(fd.Id) AS LegacyFieldCount,
    CASE WHEN COUNT(fd.Id) > 0
         THEN N'RECOVERABLE_FROM_LEGACY_FIELDS'
         ELSE N'FILE_BACKUP_REQUIRED'
    END AS RecoveryStatus
FROM dbo.Schemas AS s
LEFT JOIN dbo.FieldDefinitions AS fd ON fd.SchemaId = s.Id
WHERE s.SchemaStorageKey IS NOT NULL
GROUP BY s.Id, s.Name, s.SchemaStorageKey
ORDER BY s.Name;
GO

/*
    After checking the classification and backing up SQL, this opt-in repair
    clears file references only for selected schemas that still have legacy
    field rows. The API reconstructs and writes the JSON document at next read.
    Uncomment and replace the example ID with a SchemaId from the result above.

    DECLARE @SchemaId uniqueidentifier = '00000000-0000-0000-0000-000000000000';
    BEGIN TRANSACTION;
    UPDATE s
       SET SchemaStorageKey = NULL
    FROM dbo.Schemas AS s
    WHERE s.Id = @SchemaId
      AND s.SchemaStorageKey IS NOT NULL
      AND EXISTS (SELECT 1 FROM dbo.FieldDefinitions AS fd WHERE fd.SchemaId = s.Id);
    SELECT @@ROWCOUNT AS RowsReset;
    -- COMMIT TRANSACTION; -- run instead of ROLLBACK only after RowsReset = 1
    ROLLBACK TRANSACTION;
*/
