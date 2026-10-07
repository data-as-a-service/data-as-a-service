namespace Daas.Api.Data.Entities;

public class ApiLink
{
    public Guid Id { get; set; }

    public Guid SchemaId { get; set; }

    public string KeyHash { get; set; } = string.Empty;

    public bool IsActive { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? ExpiresAt { get; set; }

    public int DefaultRecordCount { get; set; }
}
