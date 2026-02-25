namespace Daas.Application.Users.Queries;

public class FloatGenerator: IFieldValueGenerator

{
    private Random _random;
    public FloatGenerator(Random random)
    {
        _random = random;
    }

    public object Generator()
    {
        float min = 0.0f;
        float max = 2000.0f;
        float randomfloat = (float)(_random.NextDouble()*(max-min) + min); 
        return randomfloat;
    }
}