using Daas.Api.Storage;
using Daas.Application.Users.Queries;
using Daas.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Dynamic;

namespace Daas.Api.Controllers;

[ApiController]
[Route("mock")]
public class MockController : ControllerBase
{
    private readonly FieldGeneratorFactory _factory;
    private readonly AppDbContext _context;

    public MockController(FieldGeneratorFactory factory, AppDbContext context)
    {
        _factory = factory;
        _context = context;
    }
    [HttpGet("{id}")]
    public IActionResult GetMockData(Guid id)
    {
        var schema = _context.Schemas
            .Include(x => x.Fields)
            .FirstOrDefault(x => x.Id == id);

        if (schema == null)
        {
            return NotFound();
        }

        int howmany = 10;

        var result = new List<object>();

        for (int i = 0; i < howmany; i++)
        {
            var row =
                new ExpandoObject()
                as IDictionary<string, object>;

            foreach (var field in schema.Fields)
            {
                var value =
                    _factory
                        .Get((FieldType)field.FieldType)
                        .Generator();

                row.Add(field.FieldName, value);
            }

            result.Add(row);
        }

        return Ok(result);
    }
}