using Daas.Domain.Entities;

namespace Daas.Api.Generation;

internal static class AiGenerationJsonSchema
{
    public static object Build(Schema schema, int count)
    {
        var properties = schema.Fields.ToDictionary(
            field => field.FieldName,
            field => (object)(field.FieldType switch
            {
                FieldTypes.INT => new { type = "integer" },
                FieldTypes.FLOAT or FieldTypes.DOUBLE => new { type = "number" },
                FieldTypes.BOOLEAN => new { type = "boolean" },
                FieldTypes.GUID or FieldTypes.UUID => new { type = "string", format = "uuid" },
                FieldTypes.DATE => new { type = "string", format = "date" },
                FieldTypes.DATETIME => new { type = "string", format = "date-time" },
                FieldTypes.CHAR => new { type = "string" },
                _ => new { type = "string" }
            }),
            StringComparer.Ordinal);

        return new
        {
            type = "array",
            minItems = count,
            maxItems = count,
            items = new
            {
                type = "object",
                properties,
                required = schema.Fields.Select(field => field.FieldName).ToArray(),
                additionalProperties = false
            }
        };
    }
}
