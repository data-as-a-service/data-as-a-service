namespace Daas.Domain.Entities;

public class Schema
{
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public List<FieldDefinition> Fields { get; set; }
        = new();
}