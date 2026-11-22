using Daas.Api.Services;
using Daas.Domain.Entities;
using Microsoft.AspNetCore.RateLimiting;
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

    [HttpPost("{id}/data")]
    [EnableRateLimiting("dataset-regeneration")]
    public async Task<IActionResult> GenerateDataWithMode(
        Guid id,
        [FromQuery] int count,
        [FromQuery] int generationMode,
        CancellationToken cancellationToken)
    {
        if (count is < 1 or > 1000)
            return BadRequest(new { error = "Record count must be between 1 and 1000." });
        if (generationMode is not (0 or 1))
            return BadRequest(new { error = "Generation mode must be 0 (random) or 1 (AI)." });

        try
        {
            var result = await _schemaService.GenerateDataForLinkAsync(id, count, generationMode, cancellationToken);
            return result is null ? NotFound() : Ok(result);
        }
        catch (NotSupportedException exception)
        {
            return BadRequest(new { error = exception.Message });
        }
    }
}
