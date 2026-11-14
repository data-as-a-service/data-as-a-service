using System.Text.Json;
using System.Text.Json.Nodes;
using Daas.Api.Services;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace Daas.Api.Tests;

public sealed class JsonFileStorageServiceTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), "daas-json-tests", Guid.NewGuid().ToString("N"));

    [Fact]
    public async Task SavesAndLoadsNestedDatasetJson()
    {
        var storage = CreateStorage();
        var key = Guid.NewGuid();
        var expected = JsonNode.Parse("{\"profile\":{\"name\":\"Ada\",\"tags\":[\"a\",null]},\"events\":[[{\"ok\":true}],[]],\"score\":1.5}")!;

        await storage.SaveDatasetAsync(key, expected);
        var actual = await storage.LoadDatasetAsync(key);

        Assert.NotNull(actual);
        Assert.Equal(expected.ToJsonString(), actual.ToJsonString());
    }

    [Fact]
    public async Task SavesAndLoadsSchemaDocument()
    {
        var storage = CreateStorage();
        var key = Guid.NewGuid();
        var expected = JsonNode.Parse("{\"name\":\"Example\",\"definition\":{\"type\":\"object\",\"properties\":{\"items\":{\"type\":\"array\",\"items\":{\"type\":\"number\"}}}}}")!;

        await storage.SaveSchemaAsync(key, expected);
        var actual = await storage.LoadSchemaAsync(key);

        Assert.NotNull(actual);
        Assert.Equal(expected.ToJsonString(), actual.ToJsonString());
    }

    [Fact]
    public async Task RejectsCorruptedStoredJson()
    {
        var storage = CreateStorage();
        var key = Guid.NewGuid();
        var file = Path.Combine(_root, "datasets", $"{key:N}.json");
        Directory.CreateDirectory(Path.GetDirectoryName(file)!);
        await File.WriteAllTextAsync(file, "{broken json");

        await Assert.ThrowsAnyAsync<JsonException>(() => storage.LoadDatasetAsync(key));
    }

    [Fact]
    public async Task ReturnsNullForMissingDocument()
    {
        var storage = CreateStorage();

        Assert.Null(await storage.LoadSchemaAsync(Guid.NewGuid()));
    }

    [Fact]
    public async Task RejectsDocumentOverConfiguredSizeLimit()
    {
        var storage = CreateStorage(maxBytes: 8);

        await Assert.ThrowsAsync<InvalidDataException>(() => storage.SaveSchemaAsync(Guid.NewGuid(), JsonNode.Parse("{\"value\":\"too large\"}")!));
    }

    [Fact]
    public async Task ReportsWriteFailureWhenStorageRootIsNotADirectory()
    {
        await File.WriteAllTextAsync(_root, "occupies expected directory path");
        var storage = CreateStorage();

        await Assert.ThrowsAnyAsync<IOException>(() => storage.SaveSchemaAsync(Guid.NewGuid(), JsonNode.Parse("{}")!));
    }

    [Fact]
    public async Task ConcurrentWritesLeaveACompleteJsonDocument()
    {
        var storage = CreateStorage();
        var documents = Enumerable.Range(0, 10)
            .Select(index => (Key: Guid.NewGuid(), Value: index))
            .ToArray();
        var writes = documents.Select(item =>
            storage.SaveDatasetAsync(item.Key, JsonNode.Parse($"{{\"value\":{item.Value}}}")!));

        await Task.WhenAll(writes);
        foreach (var item in documents)
        {
            var loaded = await storage.LoadDatasetAsync(item.Key);
            Assert.Equal(item.Value, loaded!["value"]!.GetValue<int>());
        }
        Assert.Empty(Directory.GetFiles(Path.Combine(_root, "datasets"), "*.tmp"));
    }

    [Fact]
    public async Task RejectsEmptyStorageIdentifier()
    {
        var storage = CreateStorage();

        await Assert.ThrowsAsync<ArgumentException>(() => storage.LoadDatasetAsync(Guid.Empty));
    }

    private JsonFileStorageService CreateStorage(long? maxBytes = null)
    {
        var values = new Dictionary<string, string?> { ["JsonStorage:RootPath"] = _root };
        if (maxBytes.HasValue) values["JsonStorage:MaxDocumentBytes"] = maxBytes.Value.ToString();
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(values).Build();
        return new JsonFileStorageService(configuration);
    }

    public void Dispose()
    {
        if (Directory.Exists(_root)) Directory.Delete(_root, recursive: true);
        else if (File.Exists(_root)) File.Delete(_root);
    }
}
