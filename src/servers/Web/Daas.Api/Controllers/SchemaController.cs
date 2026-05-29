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
}