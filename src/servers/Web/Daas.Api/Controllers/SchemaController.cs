using Daas.Api.Services;
using Daas.Domain.Entities;
using Microsoft.AspNetCore.Mvc;

namespace Daas.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SchemaController : ControllerBase
{
    private readonly SchemaService _schemaService;

    public SchemaController(SchemaService schemaService)
    {
        _schemaService = schemaService;
    }

    [HttpPost]
    public IActionResult CreateSchema([FromBody] Schema schema)
    {
        var id = _schemaService.CreateSchema(schema);
        return Ok(new { Id = id });
    }

    [HttpGet("{id}")]
    public IActionResult GetSchema(Guid id)
    {
        var schema = _schemaService.GetSchema(id);
        if (schema == null)
        {
            return NotFound();
        }

        return Ok(schema);
    }

    [HttpGet]
    public IActionResult GetAllSchemas()
    {
        return Ok(_schemaService.GetAllSchemas());
    }

    [HttpDelete("{id}")]
    public IActionResult DeleteSchema(Guid id)
    {
        if (!_schemaService.DeleteSchema(id))
        {
            return NotFound();
        }

        return NoContent();
    }

    [HttpGet("{id}/data/{howmany}")]
    public IActionResult GenerateData(Guid id, int howmany)
    {
        try
        {
            var result = _schemaService.GenerateData(id, howmany);
            if (result == null)
            {
                return NotFound();
            }

            return Ok(result);
        }
        catch (NotSupportedException exception)
        {
            return BadRequest(new { error = exception.Message });
        }
    }
}
