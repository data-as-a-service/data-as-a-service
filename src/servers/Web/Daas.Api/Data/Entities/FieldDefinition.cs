using Daas.Domain.Entities;
using System.Text.Json.Serialization;

public class FieldDefinition
{
    public int Id { get; set; }

    public string FieldName { get; set; } = string.Empty;

    public FieldTypes FieldType { get; set; }

    public Guid SchemaId { get; set; }

    [JsonIgnore]
    public Schema? Schema { get; set; }
}