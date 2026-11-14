using System.Net.Http.Json;
using System.Text.Json;
using Daas.Domain.Entities;

namespace Daas.Api.Generation;

public sealed class OllamaDataGenerator : IAiDataGenerator
{
    private const int MaxResponseCharacters = 4 * 1024 * 1024;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _configuration;
    private readonly ILogger<OllamaDataGenerator> _logger;

    public OllamaDataGenerator(IHttpClientFactory httpClientFactory, IConfiguration configuration, ILogger<OllamaDataGenerator> logger)
    {
        _httpClientFactory = httpClientFactory;
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
            var prompt = AiGenerationPrompt.Build(schema, count);
            var client = _httpClientFactory.CreateClient("Ollama");
            using var response = await client.PostAsJsonAsync($"{baseUrl.TrimEnd('/')}/api/generate", new
            {
                model,
                prompt,
                stream = false,
                format = AiGenerationJsonSchema.Build(schema, count)
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
            var result = AiGenerationOutputValidator.TryValidate(generatedText.GetString()!, schema, count);
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

    public static List<object>? TryValidateOutput(string json, Schema schema, int count)
        => AiGenerationOutputValidator.TryValidate(json, schema, count);
}
