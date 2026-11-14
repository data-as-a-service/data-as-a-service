using System.Net.Http.Json;
using System.Text.Json;
using Daas.Domain.Entities;

namespace Daas.Api.Generation;

public sealed class GoogleAiStudioDataGenerator : IAiDataGenerator
{
    private const int MaxResponseCharacters = 4 * 1024 * 1024;
    private const string DefaultModel = "gemini-3.5-flash-lite";
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _configuration;
    private readonly ILogger<GoogleAiStudioDataGenerator> _logger;

    public GoogleAiStudioDataGenerator(
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration,
        ILogger<GoogleAiStudioDataGenerator> logger)
    {
        _httpClientFactory = httpClientFactory;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<List<object>?> TryGenerateAsync(Schema schema, int count, CancellationToken cancellationToken)
    {
        var apiKey = _configuration["GoogleAiStudio:ApiKey"];
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            _logger.LogWarning("Google AI Studio is selected but no API key is configured; using the built-in generator.");
            return null;
        }

        try
        {
            var model = _configuration["GoogleAiStudio:Model"] ?? DefaultModel;
            var endpoint = _configuration["GoogleAiStudio:Endpoint"]
                ?? "https://generativelanguage.googleapis.com/v1beta/models";
            var client = _httpClientFactory.CreateClient("GoogleAiStudio");
            using var request = new HttpRequestMessage(HttpMethod.Post,
                $"{endpoint.TrimEnd('/')}/{Uri.EscapeDataString(model)}:generateContent");
            request.Headers.Add("x-goog-api-key", apiKey);
            request.Content = JsonContent.Create(new
            {
                contents = new[] { new { parts = new[] { new { text = AiGenerationPrompt.Build(schema, count) } } } },
                generationConfig = new
                {
                    responseMimeType = "application/json",
                    responseJsonSchema = AiGenerationJsonSchema.Build(schema, count)
                }
            });

            using var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
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
            if (!envelope.RootElement.TryGetProperty("candidates", out var candidates) ||
                candidates.ValueKind != JsonValueKind.Array || candidates.GetArrayLength() == 0 ||
                !candidates[0].TryGetProperty("content", out var content) ||
                !content.TryGetProperty("parts", out var parts) || parts.ValueKind != JsonValueKind.Array)
            {
                _logger.LogWarning("Google AI Studio returned no text candidate; using the built-in generator.");
                return null;
            }

            var generatedText = parts.EnumerateArray()
                .Where(part => part.TryGetProperty("text", out _))
                .Select(part => part.GetProperty("text").GetString())
                .FirstOrDefault(text => text is not null);
            if (generatedText is null) return null;

            var result = AiGenerationOutputValidator.TryValidate(generatedText, schema, count);
            if (result is null)
                _logger.LogWarning("Google AI Studio returned output that did not match the requested schema; using the built-in generator.");
            return result;
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            _logger.LogWarning("Google AI Studio generation timed out; using the built-in generator.");
            return null;
        }
        catch (Exception exception) when (exception is HttpRequestException or JsonException or InvalidOperationException or UriFormatException or IOException)
        {
            // Avoid logging request/response content or configuration values, which could contain credentials.
            _logger.LogWarning("Google AI Studio generation failed ({FailureType}); using the built-in generator.", exception.GetType().Name);
            return null;
        }
    }
}
