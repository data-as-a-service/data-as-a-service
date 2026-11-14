using System.Text.Json;
using Daas.Domain.Entities;

namespace Daas.Api.Generation;

internal static class AiGenerationPrompt
{
    public static string Build(Schema schema, int count)
    {
        var fields = string.Join(", ", schema.Fields.Select(field => $"{JsonSerializer.Serialize(field.FieldName)}: {field.FieldType}"));
        return $"Return exactly {count} realistic JSON objects as an array. Each object must contain exactly these fields with values matching their types: {{{fields}}}. Return JSON only.";
    }
}
