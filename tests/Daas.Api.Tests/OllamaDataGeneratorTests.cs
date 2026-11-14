using Daas.Api.Generation;
using Daas.Domain.Entities;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using System.Net;
using System.Text;
using Xunit;

namespace Daas.Api.Tests;

public class OllamaDataGeneratorTests
{
    private static Schema TestSchema() => new()
    {
        Fields =
        [
            new FieldDefinition { FieldName = "name", FieldType = FieldTypes.STRING },
            new FieldDefinition { FieldName = "age", FieldType = FieldTypes.INT },
            new FieldDefinition { FieldName = "active", FieldType = FieldTypes.BOOLEAN }
        ]
    };

    [Fact]
    public void AcceptsCorrectFlatArray()
    {
        var result = OllamaDataGenerator.TryValidateOutput("[{\"name\":\"Ari\",\"age\":31,\"active\":true}]", TestSchema(), 1);

        Assert.NotNull(result);
        Assert.Single(result);
    }

    [Theory]
    [InlineData("{}")]
    [InlineData("[]")]
    [InlineData("[{\"name\":\"Ari\",\"age\":\"31\",\"active\":true}]")]
    [InlineData("[{\"name\":\"Ari\",\"age\":31,\"active\":true,\"extra\":1}]")]
    [InlineData("[{\"name\":\"Ari\",\"age\":31}]")]
    [InlineData("not json")]
    public void RejectsInvalidOrMismatchedOutput(string json)
    {
        Assert.Null(OllamaDataGenerator.TryValidateOutput(json, TestSchema(), 1));
    }

    [Fact]
    public async Task ReturnsNullWhenOllamaReturnsAnError()
    {
        using var client = new HttpClient(new StubHandler(_ => new HttpResponseMessage(HttpStatusCode.ServiceUnavailable)));
        var generator = CreateGenerator(client);

        Assert.Null(await generator.TryGenerateAsync(TestSchema(), 1, CancellationToken.None));
    }

    [Fact]
    public async Task ReturnsNullWhenOllamaTimesOut()
    {
        using var client = new HttpClient(new StubHandler(_ => throw new TaskCanceledException("timeout")));
        var generator = CreateGenerator(client);

        Assert.Null(await generator.TryGenerateAsync(TestSchema(), 1, CancellationToken.None));
    }

    [Fact]
    public async Task ReturnsNullWhenOllamaOutputDoesNotMatchSchema()
    {
        using var client = new HttpClient(new StubHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("{\"response\":\"[{\\\"name\\\":\\\"Ari\\\",\\\"age\\\":\\\"thirty\\\",\\\"active\\\":true}]\"}", Encoding.UTF8, "application/json")
        }));
        var generator = CreateGenerator(client);

        Assert.Null(await generator.TryGenerateAsync(TestSchema(), 1, CancellationToken.None));
    }

    [Fact]
    public async Task AcceptsSchemaMatchingOllamaResponse()
    {
        using var client = new HttpClient(new StubHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("{\"response\":\"[{\\\"name\\\":\\\"Ari\\\",\\\"age\\\":31,\\\"active\\\":true}]\"}", Encoding.UTF8, "application/json")
        }));
        var generator = CreateGenerator(client);

        var result = await generator.TryGenerateAsync(TestSchema(), 1, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Single(result);
    }

    private static OllamaDataGenerator CreateGenerator(HttpClient client)
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Ollama:Enabled"] = "true",
            ["Ollama:BaseUrl"] = "http://ollama.test",
            ["Ollama:Model"] = "test-model"
        }).Build();
        return new OllamaDataGenerator(client, configuration, NullLogger<OllamaDataGenerator>.Instance);
    }

    private sealed class StubHandler(Func<HttpRequestMessage, HttpResponseMessage> respond) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            Task.FromResult(respond(request));
    }
}
