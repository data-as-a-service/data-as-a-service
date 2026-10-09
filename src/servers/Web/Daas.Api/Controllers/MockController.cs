using Daas.Api.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Daas.Api.Controllers;

[ApiController]
[Route("api/v1/data")]
public class MockController : ControllerBase
{
    private readonly ApiLinkService _linkService;
    private readonly SchemaService _schemaService;

    public MockController(ApiLinkService linkService, SchemaService schemaService)
    {
        _linkService = linkService;
        _schemaService = schemaService;
    }

    [HttpGet("{publicKey}")]
    [EnableRateLimiting("public-mock")]
    public async Task<IActionResult> Get(string publicKey, [FromQuery] int? count, CancellationToken cancellationToken)
    {
        if (!ApiLinkService.ValidPublicKey(publicKey)) return NotFound();
        var link = _linkService.GetActiveByKey(publicKey);
        if (link is null) return NotFound();
        var recordCount = count ?? link.DefaultRecordCount;
        if (!ApiLinkService.ValidCount(recordCount))
            return BadRequest(new { error = $"Count must be between 1 and {ApiLinkService.MaxRecordCount}." });
        var data = await _schemaService.GenerateDataForLinkAsync(link.SchemaId, recordCount, link.GenerationMode, cancellationToken);
        return data is null ? NotFound() : Ok(data);
    }
}
