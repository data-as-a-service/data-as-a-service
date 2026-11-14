using Daas.Api.Generation;
using Daas.Domain.Entities;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using System.Net;
using System.Text;
using System.Text.Json;
using Xunit;

namespace Daas.Api.Tests;

public sealed class GoogleAiStudioDataGeneratorTests
{
    private static Schema TestSchema() => new()
    {
        Fields =
        [
            new FieldDefinition { FieldName = "name", FieldType = FieldTypes.STRING },
            new FieldDefinition { FieldName = "age", FieldType = FieldTypes.INT }
        ]
    };

    [Fact]
    public async Task SendsSchemaPromptAndApiKeyAndAcceptsValidResponse()
    {
        HttpRequestMessage? capturedRequest = null;
        string? capturedBody = null;
        using var client = new HttpClient(new StubHandler(request =>
        {
            capturedRequest = request;
            capturedBody = request.Content!.ReadAsStringAsync().GetAwaiter().GetResult();
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("{\"candidates\":[{\"content\":{\"parts\":[{\"text\":\"[{\\\"name\\\":\\\"Ari\\\",\\\"age\\\":31}]\"}]}}]}", Encoding.UTF8, "application/json")
            };
        }));
        var generator = CreateGenerator(client, apiKey: "secret-test-key");

        var result = await generator.TryGenerateAsync(TestSchema(), 1, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Single(result);
        Assert.Equal("secret-test-key", capturedRequest!.Headers.GetValues("x-goog-api-key").Single());
        Assert.Contains("gemini-3.5-flash-lite:generateContent", capturedRequest.RequestUri!.ToString());
        Assert.Contains("application/json", capturedBody);
        using var requestBody = JsonDocument.Parse(capturedBody!);
        var prompt = requestBody.RootElement.GetProperty("contents")[0].GetProperty("parts")[0].GetProperty("text").GetString();
        Assert.Contains("\"name\": STRING", prompt);
        Assert.Contains("exactly 1", prompt);
    }

    [Fact]
    public async Task ReturnsNullWhenApiKeyIsMissing()
    {
        using var client = new HttpClient(new StubHandler(_ => throw new InvalidOperationException("request should not be sent")));
        var generator = CreateGenerator(client, apiKey: null);

        Assert.Null(await generator.TryGenerateAsync(TestSchema(), 1, CancellationToken.None));
    }

    [Fact]
    public async Task ReturnsNullWhenGoogleReturnsAnError()
    {
        using var client = new HttpClient(new StubHandler(_ => new HttpResponseMessage(HttpStatusCode.TooManyRequests)));
        var generator = CreateGenerator(client, apiKey: "secret-test-key");

        Assert.Null(await generator.TryGenerateAsync(TestSchema(), 1, CancellationToken.None));
    }

    [Fact]
    public async Task ReturnsNullWhenGoogleOutputDoesNotMatchSchema()
    {
        using var client = new HttpClient(new StubHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("{\"candidates\":[{\"content\":{\"parts\":[{\"text\":\"[{\\\"name\\\":\\\"Ari\\\",\\\"age\\\":\\\"thirty\\\"}]\"}]}}]}", Encoding.UTF8, "application/json")
        }));
        var generator = CreateGenerator(client, apiKey: "secret-test-key");

        Assert.Null(await generator.TryGenerateAsync(TestSchema(), 1, CancellationToken.None));
    }

    private static GoogleAiStudioDataGenerator CreateGenerator(HttpClient client, string? apiKey)
    {
        var values = new Dictionary<string, string?>
        {
            ["GoogleAiStudio:ApiKey"] = apiKey,
            ["GoogleAiStudio:Model"] = "gemini-3.5-flash-lite"
        };
        return new GoogleAiStudioDataGenerator(
            new StaticHttpClientFactory(client),
            new ConfigurationBuilder().AddInMemoryCollection(values).Build(),
            NullLogger<GoogleAiStudioDataGenerator>.Instance);
    }

    private sealed class StaticHttpClientFactory(HttpClient client) : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) => client;
    }

    private sealed class StubHandler(Func<HttpRequestMessage, HttpResponseMessage> respond) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            Task.FromResult(respond(request));
    }
}
