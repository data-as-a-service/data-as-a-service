using System.Dynamic;
using System.Text.Json;
using System.Text.Json.Nodes;
using Daas.Api.Generation;
using Daas.Domain.Entities;
using Dapper;
using Microsoft.Data.SqlClient;

namespace Daas.Api.Services;

public class SchemaService
{
    private readonly string _connectionString;
    private readonly FieldGeneratorFactory _factory;
    private readonly JsonFileStorageService _storage;
    private readonly ILogger<SchemaService> _logger;

    public SchemaService(IConfiguration configuration, FieldGeneratorFactory factory, JsonFileStorageService storage, ILogger<SchemaService> logger)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("The DefaultConnection connection string is not configured.");
        _factory = factory;
        _storage = storage;
        _logger = logger;
    }

    public async Task<Guid> CreateSchemaAsync(Schema schema, CancellationToken cancellationToken = default)
    {
        schema.Id = Guid.NewGuid();
        for (var index = 0; index < schema.Fields.Count; index++)
        {
            schema.Fields[index].Id = index + 1;
            schema.Fields[index].SchemaId = schema.Id;
        }
        var storageKey = Guid.NewGuid();
        var document = JsonSerializer.SerializeToNode(schema)
            ?? throw new InvalidDataException("Schema could not be represented as JSON.");
        await _storage.SaveSchemaAsync(storageKey, document, cancellationToken);
        try
        {
            await using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync(cancellationToken);
            using var transaction = connection.BeginTransaction();
            await connection.ExecuteAsync(
                "INSERT INTO dbo.Schemas (Id, Name, SchemaStorageKey, SchemaVersion) VALUES (@Id, @Name, @StorageKey, 1);",
                new { schema.Id, schema.Name, StorageKey = storageKey }, transaction);
            transaction.Commit();
            return schema.Id;
        }
        catch
        {
            await _storage.DeleteSchemaAsync(storageKey, CancellationToken.None);
            throw;
        }
    }

    public async Task<Schema?> GetSchemaAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await using var connection = new SqlConnection(_connectionString);
        var metadata = await connection.QuerySingleOrDefaultAsync<SchemaMetadata>(
            "SELECT Id, Name, SchemaStorageKey, SchemaVersion FROM dbo.Schemas WHERE Id = @Id;", new { Id = id });
        if (metadata is null) return null;

        if (metadata.SchemaStorageKey is Guid key)
        {
            var document = await _storage.LoadSchemaAsync(key, cancellationToken)
                ?? throw new FileNotFoundException("The schema document referenced by SQL metadata is missing.");
            var schema = document.Deserialize<Schema>()
                ?? throw new InvalidDataException("The stored schema document is invalid.");
            schema.Id = metadata.Id;
            schema.Name = metadata.Name;
            return schema;
        }

        // Existing installations have flat fields in SQL. Export each schema on first read
        // and leave the legacy rows intact for rollback and recovery.
        var legacy = new Schema { Id = metadata.Id, Name = metadata.Name };
        legacy.Fields = (await connection.QueryAsync<FieldDefinition>(
            "SELECT Id, FieldName, FieldType, SchemaId FROM dbo.FieldDefinitions WHERE SchemaId = @SchemaId ORDER BY Id;",
            new { SchemaId = id })).ToList();
        var newKey = Guid.NewGuid();
        var legacyDocument = JsonSerializer.SerializeToNode(legacy)!;
        await _storage.SaveSchemaAsync(newKey, legacyDocument, cancellationToken);
        try
        {
            var updated = await connection.ExecuteAsync(
                "UPDATE dbo.Schemas SET SchemaStorageKey = @StorageKey WHERE Id = @Id AND SchemaStorageKey IS NULL;",
                new { StorageKey = newKey, Id = id });
            if (updated == 0)
            {
                await _storage.DeleteSchemaAsync(newKey, CancellationToken.None);
                return await GetSchemaAsync(id, cancellationToken);
            }
        }
        catch
        {
            await _storage.DeleteSchemaAsync(newKey, CancellationToken.None);
            throw;
        }
        return legacy;
    }

    public async Task<List<Schema>> GetAllSchemasAsync(CancellationToken cancellationToken = default)
    {
        await using var connection = new SqlConnection(_connectionString);
        var ids = (await connection.QueryAsync<Guid>("SELECT Id FROM dbo.Schemas ORDER BY Name;")).ToList();
        var schemas = new List<Schema>(ids.Count);
        foreach (var id in ids)
        {
            var schema = await GetSchemaAsync(id, cancellationToken);
            if (schema is not null) schemas.Add(schema);
        }
        return schemas;
    }

    public async Task<bool> DeleteSchemaAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        var exists = await connection.ExecuteScalarAsync<bool>("SELECT CAST(CASE WHEN EXISTS (SELECT 1 FROM dbo.Schemas WHERE Id = @Id) THEN 1 ELSE 0 END AS bit);", new { Id = id });
        if (!exists) return false;
        var schemaKey = await connection.QuerySingleOrDefaultAsync<Guid?>(
            "SELECT SchemaStorageKey FROM dbo.Schemas WHERE Id = @Id;", new { Id = id });
        var datasetKeys = (await connection.QueryAsync<Guid>(
            "SELECT d.StorageKey FROM dbo.Datasets d JOIN dbo.ApiLinks l ON l.Id = d.ApiLinkId WHERE l.SchemaId = @Id;",
            new { Id = id })).ToList();
        var affected = await connection.ExecuteAsync("DELETE FROM dbo.Schemas WHERE Id = @Id;", new { Id = id });
        if (affected == 0) return false;

        if (schemaKey.HasValue)
        {
            try { await _storage.DeleteSchemaAsync(schemaKey.Value, CancellationToken.None); }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
            {
                _logger.LogWarning(exception, "Could not delete schema document {StorageKey} after deleting schema {SchemaId}.", schemaKey, id);
            }
        }
        foreach (var key in datasetKeys)
        {
            try { await _storage.DeleteDatasetAsync(key, CancellationToken.None); }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
            {
                _logger.LogWarning(exception, "Could not delete dataset document {StorageKey} after deleting schema {SchemaId}.", key, id);
            }
        }
        return true;
    }

    public async Task<int> GetSchemaVersionAsync(Guid id)
    {
        await using var connection = new SqlConnection(_connectionString);
        return await connection.QuerySingleOrDefaultAsync<int>("SELECT SchemaVersion FROM dbo.Schemas WHERE Id = @Id;", new { Id = id });
    }

    public List<object> GenerateData(Schema schema, int howmany)
    {
        var result = new List<object>();
        for (var i = 0; i < howmany; i++)
        {
            var row = new ExpandoObject() as IDictionary<string, object>;
            foreach (var field in schema.Fields)
            {
                var value = _factory.Get(field.FieldType).Generator();
                row.Add(field.FieldName, value);
            }
            result.Add(row);
        }
        return result;
    }

    private sealed record SchemaMetadata(Guid Id, string Name, Guid? SchemaStorageKey, int SchemaVersion);
}
