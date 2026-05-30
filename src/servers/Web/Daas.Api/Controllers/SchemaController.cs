using Daas.Api.Storage;
using Daas.Application.DTO.RequestDTO;
using Daas.Application.Users.Queries;
using Daas.Domain.Entities;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using System.Collections;
using System.Dynamic;
using Microsoft.EntityFrameworkCore;
using Daas.Infrastructure.Persistence;

namespace Daas.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SchemaController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly FieldGeneratorFactory factory;
    private readonly AppDbContext _context;

    public SchemaController(IMediator mediator, FieldGeneratorFactory _factory, AppDbContext context)
    {
        _mediator = mediator;
        factory = _factory;
        _context = context;
    }
    [HttpPost]
    public IActionResult CreateSchema([FromBody] Schema schema)
    {
        schema.Id = Guid.NewGuid();

        _context.Schemas.Add(schema);
        _context.SaveChanges();

        return Ok(new
        {
            schema.Id
        });
    }

    [HttpGet("{id}")]
    public IActionResult GetSchema(Guid id)
    {
        var schema = _context.Schemas
            .Include(x => x.Fields)
            .FirstOrDefault(x => x.Id == id);

        if (schema == null)
        {
            return NotFound();
        }

        return Ok(schema);
    }

    [HttpGet]
    public IActionResult GetAllSchemas()
    {
        var schemas = _context.Schemas
            .Include(x => x.Fields)
            .ToList();

        return Ok(schemas);
    }

    [HttpDelete("{id}")]
    public IActionResult DeleteSchema(Guid id)
    {
        var schema = _context.Schemas
            .Include(x => x.Fields)
            .FirstOrDefault(x => x.Id == id);

        if (schema == null)
        {
            return NotFound();
        }

        _context.Schemas.Remove(schema);

        _context.SaveChanges();

        return NoContent();
    }

    [HttpGet("{id}/data/{howmany}")]
    public IActionResult GenerateData(
        Guid id,
        int howmany)
    {
        var schema = _context.Schemas
            .Include(x => x.Fields)
            .FirstOrDefault(x => x.Id == id);

        if (schema == null)
        {
            return NotFound();
        }

        var result = new List<object>();

        for (int i = 0; i < howmany; i++)
        {
            var row =
                new ExpandoObject()
                as IDictionary<string, object>;

            foreach (var field in schema.Fields)
            {
                var value =
    factory
        .Get((Daas.Application.Users.Queries.FieldType)field.FieldType)
        .Generator();

                row.Add(field.FieldName, value);
            }

            result.Add(row);
        }

        return Ok(result);
    }
}