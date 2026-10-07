using Daas.Api.Generation;

public class DoubleGenerator : IFieldValueGenerator
{
    private readonly Random random;

    public DoubleGenerator(Random random)
    {
        this.random = random;
    }

    public object Generator()
    {
        return random.NextDouble() * 1000;
    }
}