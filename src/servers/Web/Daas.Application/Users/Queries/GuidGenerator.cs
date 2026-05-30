using Daas.Application.Users.Queries;

public class GuidGenerator : IFieldValueGenerator
{
    public object Generator()
    {
        return Guid.NewGuid();
    }
}