using Daas.Api.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Daas.Api.Controllers;

[ApiController]
[Route("api/v1/data")]
public class MockController : ControllerBase
{
    private readonly ApiLinkService _linkService;
    private readonly DatasetService _datasetService;
    private readonly ILogger<MockController> _logger;

    public MockController(ApiLinkService linkService, DatasetService datasetService, ILogger<MockController> logger)
    {
        _linkService = linkService;
        _datasetService = datasetService;
        _logger = logger;
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
        try
        {
            var data = await _datasetService.GetOrCreateAsync(link, recordCount, cancellationToken: cancellationToken);
            return data is null ? NotFound() : Ok(data);
        }
        catch (NotSupportedException exception)
        {
            return BadRequest(new { error = exception.Message });
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or InvalidDataException or System.Text.Json.JsonException)
        {
            _logger.LogError(exception, "Persisted dataset for API link {LinkId} is unavailable.", link.Id);
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new { error = "The persisted dataset is unavailable. Check the configured JSON storage and retry." });
        }
    }
}
