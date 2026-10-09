using System.Net.Http.Json;
using System.Text.Json;
using Daas.Domain.Entities;

namespace Daas.Api.Generation;

public sealed class OllamaDataGenerator
{
    private const int MaxResponseCharacters = 4 * 1024 * 1024;
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly ILogger<OllamaDataGenerator> _logger;

    public OllamaDataGenerator(HttpClient httpClient, IConfiguration configuration, ILogger<OllamaDataGenerator> logger)
    {
        _httpClient = httpClient;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<List<object>?> TryGenerateAsync(Schema schema, int count, CancellationToken cancellationToken)
    {
        if (!_configuration.GetValue<bool>("Ollama:Enabled")) return null;

        try
        {
            var baseUrl = _configuration["Ollama:BaseUrl"] ?? "http://localhost:11434";
            var model = _configuration["Ollama:Model"] ?? "llama3.2";
            var prompt = BuildPrompt(schema, count);
            using var response = await _httpClient.PostAsJsonAsync($"{baseUrl.TrimEnd('/')}/api/generate", new
            {
                model,
                prompt,
                stream = false,
                format = "json"
            }, cancellationToken);
            response.EnsureSuccessStatusCode();

            if (response.Content.Headers.ContentLength > MaxResponseCharacters) return null;
            await using var responseStream = await response.Content.ReadAsStreamAsync(cancellationToken);
            using var output = new MemoryStream();
            var buffer = new byte[8192];
            int read;
            while ((read = await responseStream.ReadAsync(buffer, cancellationToken)) > 0)
            {
                if (output.Length + read > MaxResponseCharacters) return null;
                await output.WriteAsync(buffer.AsMemory(0, read), cancellationToken);
            }
            output.Position = 0;
            using var envelope = await JsonDocument.ParseAsync(output, cancellationToken: cancellationToken);
            if (!envelope.RootElement.TryGetProperty("response", out var generatedText) || generatedText.ValueKind != JsonValueKind.String)
                return null;
            var result = TryValidateOutput(generatedText.GetString()!, schema, count);
            if (result is null) _logger.LogWarning("Ollama returned output that did not match the requested schema; using the built-in generator.");
            return result;
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            _logger.LogWarning("Ollama generation timed out; using the built-in generator.");
            return null;
        }
        catch (Exception exception) when (exception is HttpRequestException or JsonException or InvalidOperationException or UriFormatException or IOException)
        {
            _logger.LogWarning("Ollama generation failed ({FailureType}); using the built-in generator.", exception.GetType().Name);
            return null;
        }
    }

    private static string BuildPrompt(Schema schema, int count)
    {
        var fields = string.Join(", ", schema.Fields.Select(field => $"{JsonSerializer.Serialize(field.FieldName)}: {field.FieldType}"));
        return $"Return exactly {count} realistic JSON objects as an array. Each object must contain exactly these fields with values matching their types: {{{fields}}}. Return JSON only.";
    }

    public static List<object>? TryValidateOutput(string json, Schema schema, int count)
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
