using System.Text.Json;
using System.Text.Json.Nodes;
using Daas.Api.Data.Entities;
using Dapper;
using Microsoft.Data.SqlClient;

namespace Daas.Api.Services;

public sealed class DatasetService
{
    private readonly string _connectionString;
    private readonly SchemaService _schemaService;
    private readonly JsonFileStorageService _storage;
    private readonly ILogger<DatasetService> _logger;
    private readonly int _retainedVersions;
    private readonly int _lifetimeHours;
    private readonly SemaphoreSlim _generationLock = new(1, 1);

    public DatasetService(IConfiguration configuration, SchemaService schemaService, JsonFileStorageService storage, ILogger<DatasetService> logger)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("The DefaultConnection connection string is not configured.");
        _schemaService = schemaService;
        _storage = storage;
        _logger = logger;
        _retainedVersions = Math.Clamp(configuration.GetValue<int?>("JsonStorage:RetainedDatasetVersions") ?? 5, 1, 100);
        _lifetimeHours = Math.Clamp(configuration.GetValue<int?>("JsonStorage:DatasetLifetimeHours") ?? 0, 0, 87600);
    }

    public async Task<JsonNode?> GetOrCreateAsync(ApiLink link, int recordCount, bool regenerate = false, CancellationToken cancellationToken = default)
    {
        await using var lookupConnection = new SqlConnection(_connectionString);
        var current = await GetCurrentAsync(lookupConnection, link.Id, recordCount);
        if (!regenerate && IsForCurrentSchema(current))
            return await LoadCurrentAsync(current!, cancellationToken);

        // Only cache misses and explicit regenerations serialize. The follow-up lookup
        // makes simultaneous first requests share the first completed dataset.
        await _generationLock.WaitAsync(cancellationToken);
        try
        {
            current = await GetCurrentAsync(lookupConnection, link.Id, recordCount);
            if (!regenerate && IsForCurrentSchema(current))
                return await LoadCurrentAsync(current!, cancellationToken);

            var schema = await _schemaService.GetSchemaAsync(link.SchemaId, cancellationToken);
            if (schema is null) return null;
            var schemaVersion = await _schemaService.GetSchemaVersionAsync(link.SchemaId);

            var generated = _schemaService.GenerateData(schema, recordCount);
            var document = JsonSerializer.SerializeToNode(generated)
                ?? throw new InvalidDataException("Generated data could not be represented as JSON.");
            var storageKey = Guid.NewGuid();
            await _storage.SaveDatasetAsync(storageKey, document, cancellationToken);
            List<Guid> obsoleteStorageKeys;
            try
            {
                await using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync(cancellationToken);
                using var transaction = connection.BeginTransaction();
                var version = await connection.ExecuteScalarAsync<int>(
                    "SELECT ISNULL(MAX(Version), 0) + 1 FROM dbo.Datasets WITH (UPDLOCK, HOLDLOCK) WHERE ApiLinkId = @ApiLinkId AND RecordCount = @RecordCount;",
                    new { ApiLinkId = link.Id, RecordCount = recordCount }, transaction);
                await connection.ExecuteAsync(
                    "UPDATE dbo.Datasets SET IsCurrent = 0 WHERE ApiLinkId = @ApiLinkId AND RecordCount = @RecordCount AND IsCurrent = 1;",
                    new { ApiLinkId = link.Id, RecordCount = recordCount }, transaction);
                await connection.ExecuteAsync(
                    "INSERT INTO dbo.Datasets (DatasetId, ApiLinkId, SchemaId, SchemaVersion, RecordCount, Version, StorageKey, IsCurrent, CreatedAtUtc, ExpiresAtUtc) VALUES (@DatasetId, @ApiLinkId, @SchemaId, @SchemaVersion, @RecordCount, @Version, @StorageKey, 1, SYSUTCDATETIME(), @ExpiresAtUtc);",
                    new { DatasetId = Guid.NewGuid(), ApiLinkId = link.Id, link.SchemaId, SchemaVersion = schemaVersion, RecordCount = recordCount, Version = version, StorageKey = storageKey, ExpiresAtUtc = GetExpirationUtc() }, transaction);
                obsoleteStorageKeys = (await connection.QueryAsync<Guid>(
                    "SELECT StorageKey FROM dbo.Datasets WHERE ApiLinkId = @ApiLinkId AND RecordCount = @RecordCount AND Version <= @OldestVersion;",
                    new { ApiLinkId = link.Id, RecordCount = recordCount, OldestVersion = version - _retainedVersions }, transaction)).ToList();
                if (obsoleteStorageKeys.Count > 0)
                {
                    await connection.ExecuteAsync(
                        "DELETE FROM dbo.Datasets WHERE ApiLinkId = @ApiLinkId AND RecordCount = @RecordCount AND Version <= @OldestVersion;",
                        new { ApiLinkId = link.Id, RecordCount = recordCount, OldestVersion = version - _retainedVersions }, transaction);
                }
                transaction.Commit();
            }
            catch
            {
                await _storage.DeleteDatasetAsync(storageKey, CancellationToken.None);
                throw;
            }
            foreach (var obsoleteKey in obsoleteStorageKeys)
            {
                try { await _storage.DeleteDatasetAsync(obsoleteKey, cancellationToken); }
                catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
                {
                    _logger.LogWarning(exception, "Could not delete obsolete dataset file {StorageKey}.", obsoleteKey);
                }
            }
            return document;
        }
        finally
        {
            _generationLock.Release();
        }
    }

    private static Task<DatasetRecord?> GetCurrentAsync(SqlConnection connection, Guid linkId, int recordCount) =>
        connection.QuerySingleOrDefaultAsync<DatasetRecord>(
            "SELECT TOP (1) d.DatasetId, d.StorageKey, d.SchemaVersion, d.Version, s.SchemaVersion AS CurrentSchemaVersion FROM dbo.Datasets d JOIN dbo.Schemas s ON s.Id = d.SchemaId WHERE d.ApiLinkId = @ApiLinkId AND d.RecordCount = @RecordCount AND d.IsCurrent = 1 AND (d.ExpiresAtUtc IS NULL OR d.ExpiresAtUtc > SYSUTCDATETIME()) ORDER BY d.Version DESC;",
            new { ApiLinkId = linkId, RecordCount = recordCount });

    private static bool IsForCurrentSchema(DatasetRecord? record) =>
        record is not null && record.SchemaVersion == record.CurrentSchemaVersion;

    private async Task<JsonNode> LoadCurrentAsync(DatasetRecord record, CancellationToken cancellationToken) =>
        await _storage.LoadDatasetAsync(record.StorageKey, cancellationToken)
        ?? throw new FileNotFoundException("The saved dataset file is missing.");

    private sealed record DatasetRecord(Guid DatasetId, Guid StorageKey, int SchemaVersion, int Version, int CurrentSchemaVersion);

    private DateTime? GetExpirationUtc()
    {
        return _lifetimeHours > 0 ? DateTime.UtcNow.AddHours(_lifetimeHours) : null;
    }
}
