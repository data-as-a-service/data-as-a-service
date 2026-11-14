using System.Security.Cryptography;
using System.Text;
using Daas.Api.Data.Entities;
using Dapper;
using Microsoft.Data.SqlClient;

namespace Daas.Api.Services;

public class ApiLinkService
{
    public const int MaxRecordCount = 1000;

    private readonly string _connectionString;

    public ApiLinkService(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("The DefaultConnection connection string is not configured.");
    }

    public ApiLink? GetActiveByKey(string publicKey)
    {
        var hash = HashKey(publicKey);
        using var connection = new SqlConnection(_connectionString);
        return connection.QuerySingleOrDefault<ApiLink>(
            "SELECT Id, SchemaId, KeyHash, IsActive, CreatedAt, ExpiresAt, DefaultRecordCount FROM dbo.ApiLinks WHERE KeyHash = @KeyHash AND IsActive = 1 AND (ExpiresAt IS NULL OR ExpiresAt > SYSUTCDATETIME());",
            new { KeyHash = hash });
    }

    public List<ApiLink> GetBySchema(Guid schemaId)
    {
        using var connection = new SqlConnection(_connectionString);
        return connection.Query<ApiLink>(
            "SELECT Id, SchemaId, KeyHash, IsActive, CreatedAt, ExpiresAt, DefaultRecordCount FROM dbo.ApiLinks WHERE SchemaId = @SchemaId ORDER BY CreatedAt DESC;",
            new { SchemaId = schemaId }).ToList();
    }

    public ApiLink? GetActive(Guid schemaId, Guid linkId)
    {
        using var connection = new SqlConnection(_connectionString);
        return connection.QuerySingleOrDefault<ApiLink>(
            "SELECT Id, SchemaId, KeyHash, IsActive, CreatedAt, ExpiresAt, DefaultRecordCount FROM dbo.ApiLinks WHERE Id = @LinkId AND SchemaId = @SchemaId AND IsActive = 1 AND (ExpiresAt IS NULL OR ExpiresAt > SYSUTCDATETIME());",
            new { LinkId = linkId, SchemaId = schemaId });
    }

    public (ApiLink? Link, string? PublicKey) Create(Guid schemaId, int count, DateTime? expiresAt)
    {
        ValidateSettings(count, expiresAt);
        var publicKey = Convert.ToHexString(RandomNumberGenerator.GetBytes(32)).ToLowerInvariant();
        var link = new ApiLink
        {
            Id = Guid.NewGuid(),
            SchemaId = schemaId,
            KeyHash = HashKey(publicKey),
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = expiresAt,
            DefaultRecordCount = count
        };

        using var connection = new SqlConnection(_connectionString);
        var affected = connection.Execute(
            "INSERT INTO dbo.ApiLinks (Id, SchemaId, KeyHash, IsActive, CreatedAt, ExpiresAt, DefaultRecordCount) SELECT @Id, @SchemaId, @KeyHash, @IsActive, @CreatedAt, @ExpiresAt, @DefaultRecordCount WHERE EXISTS (SELECT 1 FROM dbo.Schemas WHERE Id = @SchemaId);",
            link);
        if (affected == 0)
        {
            return (null, null);
        }

        return (link, publicKey);
    }

    public ApiLink? Update(Guid schemaId, Guid linkId, int count, DateTime? expiresAt)
    {
        ValidateSettings(count, expiresAt);
        using var connection = new SqlConnection(_connectionString);
        var updated = connection.Execute(
            "UPDATE dbo.ApiLinks SET DefaultRecordCount = @Count, ExpiresAt = @ExpiresAt WHERE Id = @LinkId AND SchemaId = @SchemaId AND IsActive = 1;",
            new { LinkId = linkId, SchemaId = schemaId, Count = count, ExpiresAt = expiresAt });
        if (updated == 0) return null;
        return connection.QuerySingle<ApiLink>(
            "SELECT Id, SchemaId, KeyHash, IsActive, CreatedAt, ExpiresAt, DefaultRecordCount FROM dbo.ApiLinks WHERE Id = @LinkId;",
            new { LinkId = linkId });
    }

    public bool Revoke(Guid schemaId, Guid linkId)
    {
        using var connection = new SqlConnection(_connectionString);
        return connection.Execute(
            "UPDATE dbo.ApiLinks SET IsActive = 0 WHERE Id = @LinkId AND SchemaId = @SchemaId AND IsActive = 1;",
            new { LinkId = linkId, SchemaId = schemaId }) > 0;
    }

    public (ApiLink? Link, string? PublicKey) Rotate(Guid schemaId, Guid linkId)
    {
        var publicKey = Convert.ToHexString(RandomNumberGenerator.GetBytes(32)).ToLowerInvariant();
        using var connection = new SqlConnection(_connectionString);
        using var transaction = connection.BeginTransaction();
        var updated = connection.Execute(
            "UPDATE dbo.ApiLinks SET KeyHash = @KeyHash WHERE Id = @LinkId AND SchemaId = @SchemaId AND IsActive = 1;",
            new { LinkId = linkId, SchemaId = schemaId, KeyHash = HashKey(publicKey) }, transaction);
        if (updated == 0) return (null, null);
        var link = connection.QuerySingle<ApiLink>(
            "SELECT Id, SchemaId, KeyHash, IsActive, CreatedAt, ExpiresAt, DefaultRecordCount FROM dbo.ApiLinks WHERE Id = @LinkId;",
            new { LinkId = linkId }, transaction);
        transaction.Commit();
        return (link, publicKey);
    }

    public static string HashKey(string publicKey) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(publicKey))).ToLowerInvariant();

    public static bool ValidCount(int count) => count is >= 1 and <= MaxRecordCount;

    public static bool ValidPublicKey(string publicKey) =>
        publicKey.Length == 64 && publicKey.All(Uri.IsHexDigit);

    private static void ValidateSettings(int count, DateTime? expiresAt)
    {
        if (!ValidCount(count)) throw new ArgumentOutOfRangeException(nameof(count));
        if (expiresAt.HasValue && expiresAt.Value <= DateTime.UtcNow)
            throw new ArgumentOutOfRangeException(nameof(expiresAt));
    }
}
