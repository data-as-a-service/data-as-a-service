using Daas.Application.Users.Queries;
using Daas.Domain.Entities;
using Daas.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using System.Dynamic;

namespace Daas.Api.Services;

public class SchemaService
{
    private readonly AppDbContext _context;
    private readonly FieldGeneratorFactory _factory;

    public SchemaService(AppDbContext context, FieldGeneratorFactory factory)
    {
        _context = context;
        _factory = factory;
    }

    public Guid CreateSchema(Schema schema)
    {
        schema.Id = Guid.NewGuid();
        _context.Schemas.Add(schema);
        _context.SaveChanges();
        return schema.Id;
    }

    public Schema? GetSchema(Guid id)
    {
        return _context.Schemas
            .Include(x => x.Fields)
            .FirstOrDefault(x => x.Id == id);
    }

    public List<Schema> GetAllSchemas()
    {
        return _context.Schemas
            .Include(x => x.Fields)
            .ToList();
    }

    public bool DeleteSchema(Guid id)
    {
        var schema = GetSchema(id);
        if (schema == null)
        {
            return false;
        }

        _context.Schemas.Remove(schema);
        _context.SaveChanges();
        return true;
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

    public List<object>? GenerateMockData(Guid id)
    {
        return GenerateData(id, 50);
    }
}
