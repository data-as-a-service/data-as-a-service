using Daas.Application.Users.Queries;

public class DateGenerator : IFieldValueGenerator
{
    private readonly Random random;

    public DateGenerator(Random random)
    {
        this.random = random;
    }

    public object Generator()
    {
        return DateTime.Today.AddDays(
            random.Next(-3650, 3650)
        );
    }
}