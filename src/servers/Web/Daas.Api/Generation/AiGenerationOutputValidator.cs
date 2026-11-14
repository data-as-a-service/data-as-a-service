using System.Text.Json;
using Daas.Domain.Entities;

namespace Daas.Api.Generation;

internal static class AiGenerationOutputValidator
{
    public static List<object>? TryValidate(string json, Schema schema, int count)
    {
        try
        {
            using var document = JsonDocument.Parse(json);
            var root = document.RootElement;
            if (root.ValueKind != JsonValueKind.Array || root.GetArrayLength() != count) return null;

            var expectedFields = schema.Fields.ToDictionary(field => field.FieldName, StringComparer.Ordinal);
            var records = new List<object>(count);
            foreach (var item in root.EnumerateArray())
            {
                if (item.ValueKind != JsonValueKind.Object) return null;
                var values = new Dictionary<string, object>(StringComparer.Ordinal);
                foreach (var property in item.EnumerateObject())
                {
                    if (!expectedFields.TryGetValue(property.Name, out var field) || values.ContainsKey(property.Name) || !MatchesType(property.Value, field.FieldType))
                        return null;
                    values.Add(property.Name, property.Value.Clone());
                }

                if (values.Count != expectedFields.Count) return null;
                records.Add(values);
            }

            return records;
        }
        catch (JsonException)
        {
            return null;
        }
        catch (ArgumentException)
        {
            return null;
        }
    }

    private static bool MatchesType(JsonElement value, FieldTypes type) => type switch
    {
        FieldTypes.INT => value.ValueKind == JsonValueKind.Number && value.TryGetInt32(out _),
        FieldTypes.FLOAT => value.ValueKind == JsonValueKind.Number && value.TryGetSingle(out _),
        FieldTypes.DOUBLE => value.ValueKind == JsonValueKind.Number && value.TryGetDouble(out _),
        FieldTypes.BOOLEAN => value.ValueKind is JsonValueKind.True or JsonValueKind.False,
        FieldTypes.STRING => value.ValueKind == JsonValueKind.String,
        FieldTypes.CHAR => value.ValueKind == JsonValueKind.String && value.GetString()?.Length == 1,
        FieldTypes.GUID or FieldTypes.UUID => value.ValueKind == JsonValueKind.String && Guid.TryParse(value.GetString(), out _),
        FieldTypes.DATE or FieldTypes.DATETIME => value.ValueKind == JsonValueKind.String && DateTime.TryParse(value.GetString(), out _),
        _ => false
    };
}
