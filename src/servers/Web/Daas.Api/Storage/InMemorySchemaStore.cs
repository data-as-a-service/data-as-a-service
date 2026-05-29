using Daas.Domain.Entities;
namespace Daas.Api.Storage;

public static class InMemorySchemaStore
{
    public static Dictionary<Guid, Schema> Schemas
        = new();
}