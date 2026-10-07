using Daas.Api.Generation;
using Daas.Domain.Entities;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using System.Dynamic;

namespace Daas.Api.Services;

public class SchemaService
{
    private readonly string _connectionString;
    private readonly FieldGeneratorFactory _factory;

    public SchemaService(IConfiguration configuration, FieldGeneratorFactory factory)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("The DefaultConnection connection string is not configured.");
        _factory = factory;
    }

    public Guid CreateSchema(Schema schema)
    {
        schema.Id = Guid.NewGuid();

        using var connection = new SqlConnection(_connectionString);
        connection.Open();
        using var transaction = connection.BeginTransaction();

        connection.Execute(
            "INSERT INTO dbo.Schemas (Id, Name) VALUES (@Id, @Name);",
            new { schema.Id, schema.Name },
            transaction);

        foreach (var field in schema.Fields)
        {
            field.SchemaId = schema.Id;
            connection.Execute(
                "INSERT INTO dbo.FieldDefinitions (FieldName, FieldType, SchemaId) VALUES (@FieldName, @FieldType, @SchemaId);",
                field,
                transaction);
        }

        transaction.Commit();
        return schema.Id;
    }

    public Schema? GetSchema(Guid id)
    {
        using var connection = new SqlConnection(_connectionString);
        var schema = connection.QuerySingleOrDefault<Schema>(
            "SELECT Id, Name FROM dbo.Schemas WHERE Id = @Id;",
            new { Id = id });

        if (schema == null)
        {
            return null;
        }

        schema.Fields = connection.Query<FieldDefinition>(
            "SELECT Id, FieldName, FieldType, SchemaId FROM dbo.FieldDefinitions WHERE SchemaId = @SchemaId;",
            new { SchemaId = id }).ToList();

        return schema;
    }

    public List<Schema> GetAllSchemas()
    {
        using var connection = new SqlConnection(_connectionString);
        var schemas = connection.Query<Schema>(
            "SELECT Id, Name FROM dbo.Schemas;").ToList();
        var fieldsBySchemaId = connection.Query<FieldDefinition>(
                "SELECT Id, FieldName, FieldType, SchemaId FROM dbo.FieldDefinitions;")
            .GroupBy(field => field.SchemaId)
            .ToDictionary(group => group.Key, group => group.ToList());

        foreach (var schema in schemas)
        {
            schema.Fields = fieldsBySchemaId.TryGetValue(schema.Id, out var fields)
                ? fields
                : new List<FieldDefinition>();
        }

        return schemas;
    }

    public bool DeleteSchema(Guid id)
    {
        using var connection = new SqlConnection(_connectionString);
        return connection.Execute(
            "DELETE FROM dbo.Schemas WHERE Id = @Id;",
            new { Id = id }) > 0;
    }

    public List<object>? GenerateData(Guid id, int howmany)
    {
        var schema = GetSchema(id);
        if (schema == null)
        {
            return null;
        }

        var result = new List<object>();

        for (int i = 0; i < howmany; i++)
        {
            var row = new ExpandoObject() as IDictionary<string, object>;

            foreach (var field in schema.Fields)
            {
                var value = _factory
                    .Get((FieldType)field.FieldType)
                    .Generator();

                row.Add(field.FieldName, value);
            }

            result.Add(row);
        }

        return result;
    }
}
