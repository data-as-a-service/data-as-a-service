using Daas.Api.Contracts;
using Daas.Api.Generation;
using Daas.Domain.Entities;
using Daas.Infrastructure.Persistence;
using System.Collections;
using System.Dynamic;

namespace Daas.Api.Services;

public class UserService
{
    private readonly AppDbContext _context;
    private readonly FieldGeneratorFactory _factory;

    public UserService(AppDbContext context, FieldGeneratorFactory factory)
    {
        _context = context;
        _factory = factory;
    }

    public Task<List<User>> GetUsers()
    {
        return Task.FromResult(_context.Users.ToList());
    }

    public Task<Dummy> GetDummy()
    {
        return Task.FromResult(new Dummy
        {
            name = "aakash",
            address = "123"
        });
    }

    public Task<ArrayList> GeneratePayload(RequestModel[] fields, int howmany)
    {
        Dictionary<string, string> values = new Dictionary<string, string>();
        ArrayList data = new ArrayList();
        int iterations = howmany;

        while (iterations > 0)
        {
            var row = new ExpandoObject() as IDictionary<string, object>;
            for (int i = 0; i < fields.Length; i++)
            {
                string answer = _factory.Get(fields[i].fieldType)?.Generator()?.ToString();
                values.Add(fields[i].fieldName, answer);
                row.Add(fields[i].fieldName, answer);
            }

            data.Add(row);
            iterations--;
            values.Clear();
        }

        return Task.FromResult(data);
    }
}
