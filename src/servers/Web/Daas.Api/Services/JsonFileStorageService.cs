using System.Text.Json;
using System.Text.Json.Nodes;

namespace Daas.Api.Services;

public sealed class JsonFileStorageService
{
    private const long DefaultMaxDocumentBytes = 50 * 1024 * 1024;
    private readonly string _root;
    private readonly long _maxDocumentBytes;

    public JsonFileStorageService(IConfiguration configuration)
    {
        var configuredRoot = configuration["JsonStorage:RootPath"];
        _root = Path.GetFullPath(string.IsNullOrWhiteSpace(configuredRoot)
            ? Path.Combine(AppContext.BaseDirectory, "App_Data", "json")
            : configuredRoot);
        _maxDocumentBytes = Math.Max(1, configuration.GetValue<long?>("JsonStorage:MaxDocumentBytes") ?? DefaultMaxDocumentBytes);
    }

    public Task SaveSchemaAsync(Guid storageKey, JsonNode document, CancellationToken cancellationToken = default) =>
        SaveAsync("schemas", storageKey, document, cancellationToken);

    public Task<JsonNode?> LoadSchemaAsync(Guid storageKey, CancellationToken cancellationToken = default) =>
        LoadAsync("schemas", storageKey, cancellationToken);

    public Task DeleteSchemaAsync(Guid storageKey, CancellationToken cancellationToken = default) =>
        DeleteAsync("schemas", storageKey, cancellationToken);

    public Task SaveDatasetAsync(Guid storageKey, JsonNode document, CancellationToken cancellationToken = default) =>
        SaveAsync("datasets", storageKey, document, cancellationToken);

    public Task<JsonNode?> LoadDatasetAsync(Guid storageKey, CancellationToken cancellationToken = default) =>
        LoadAsync("datasets", storageKey, cancellationToken);

    public Task DeleteDatasetAsync(Guid storageKey, CancellationToken cancellationToken = default) =>
        DeleteAsync("datasets", storageKey, cancellationToken);

    private async Task SaveAsync(string category, Guid storageKey, JsonNode document, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(document);
        var bytes = JsonSerializer.SerializeToUtf8Bytes(document);
        ValidateJson(bytes);
        var path = GetPath(category, storageKey);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        var temporaryPath = Path.Combine(Path.GetDirectoryName(path)!, $".{storageKey:N}.{Guid.NewGuid():N}.tmp");
        try
        {
            await using (var stream = new FileStream(temporaryPath, FileMode.CreateNew, FileAccess.Write, FileShare.None, 81920,
                FileOptions.Asynchronous | FileOptions.WriteThrough))
            {
                await stream.WriteAsync(bytes, cancellationToken);
                await stream.FlushAsync(cancellationToken);
            }
            File.Move(temporaryPath, path, overwrite: true);
        }
        finally
        {
            if (File.Exists(temporaryPath)) File.Delete(temporaryPath);
        }
    }

    private async Task<JsonNode?> LoadAsync(string category, Guid storageKey, CancellationToken cancellationToken)
    {
        var path = GetPath(category, storageKey);
        if (!File.Exists(path)) return null;
        var info = new FileInfo(path);
        if (info.Length > _maxDocumentBytes) throw new InvalidDataException("Stored JSON document exceeds the configured size limit.");
        var bytes = await File.ReadAllBytesAsync(path, cancellationToken);
        ValidateJson(bytes);
        return JsonNode.Parse(bytes);
    }

    private Task DeleteAsync(string category, Guid storageKey, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var path = GetPath(category, storageKey);
        if (File.Exists(path)) File.Delete(path);
        return Task.CompletedTask;
    }

    private string GetPath(string category, Guid storageKey)
    {
        if (category is not ("schemas" or "datasets") || storageKey == Guid.Empty)
            throw new ArgumentException("Invalid internal storage key.", nameof(storageKey));
        var directory = Path.GetFullPath(Path.Combine(_root, category));
        var path = Path.GetFullPath(Path.Combine(directory, $"{storageKey:N}.json"));
        if (!path.StartsWith(directory + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Storage key resolves outside the configured storage root.");
        return path;
    }

    private void ValidateJson(byte[] bytes)
    {
        if (bytes.LongLength > _maxDocumentBytes) throw new InvalidDataException("JSON document exceeds the configured size limit.");
        using var _ = JsonDocument.Parse(bytes);
    }
}
