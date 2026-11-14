namespace Daas.Api.Contracts;

public record CreateApiLinkRequest(int DefaultRecordCount = 10, DateTime? ExpiresAt = null);

public record UpdateApiLinkRequest(int DefaultRecordCount, DateTime? ExpiresAt = null);

public record ApiLinkResponse(
    Guid Id,
    Guid SchemaId,
    bool IsActive,
    DateTime CreatedAt,
    DateTime? ExpiresAt,
    int DefaultRecordCount);

public record CreatedApiLinkResponse(
    Guid Id,
    Guid SchemaId,
    string Url,
    bool IsActive,
    DateTime CreatedAt,
    DateTime? ExpiresAt,
    int DefaultRecordCount);
