using Daas.Api.Generation;

public class GuidGenerator : IFieldValueGenerator
{
    public object Generator()
    {
        return Guid.NewGuid();
    }
}