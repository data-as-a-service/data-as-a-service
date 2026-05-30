using Daas.Application.Users.Queries;

public class CharacterGenerator : IFieldValueGenerator
{
    private readonly Random random;

    public CharacterGenerator(Random random)
    {
        this.random = random;
    }

    public object Generator()
    {
        return (char)random.Next('A', 'Z' + 1);
    }
}