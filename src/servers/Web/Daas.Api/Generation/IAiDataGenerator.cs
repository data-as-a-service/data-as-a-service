using Daas.Domain.Entities;

namespace Daas.Api.Generation;

public interface IAiDataGenerator
{
    Task<List<object>?> TryGenerateAsync(Schema schema, int count, CancellationToken cancellationToken);
}
