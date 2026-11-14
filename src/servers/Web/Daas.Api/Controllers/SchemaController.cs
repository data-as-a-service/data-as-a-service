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
    public async Task<IActionResult> CreateSchema([FromBody] Schema schema, CancellationToken cancellationToken)
    {
        var id = await _schemaService.CreateSchemaAsync(schema, cancellationToken);
        return Ok(new { Id = id });
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetSchema(Guid id, CancellationToken cancellationToken)
    {
        var schema = await _schemaService.GetSchemaAsync(id, cancellationToken);
        if (schema == null)
        {
            return NotFound();
        }

        return Ok(schema);
    }

    [HttpGet]
    public async Task<IActionResult> GetAllSchemas(CancellationToken cancellationToken)
    {
        return Ok(await _schemaService.GetAllSchemasAsync(cancellationToken));
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteSchema(Guid id, CancellationToken cancellationToken)
    {
        if (!await _schemaService.DeleteSchemaAsync(id, cancellationToken))
        {
            return NotFound();
        }

        return NoContent();
    }

    [HttpGet("{id}/data/{howmany}")]
    public async Task<IActionResult> GenerateData(Guid id, int howmany, CancellationToken cancellationToken)
    {
        try
        {
            var schema = await _schemaService.GetSchemaAsync(id, cancellationToken);
            var result = schema is null ? null : _schemaService.GenerateData(schema, howmany);
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
