using Daas.Api.Contracts;
using Daas.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace Daas.Api.Controllers;

[ApiController]
[Route("api/schema/{schemaId:guid}/links")]
public class ApiLinkController : ControllerBase
{
    private readonly SchemaService _schemaService;
    private readonly ApiLinkService _linkService;
    private readonly DatasetService _datasetService;

    public ApiLinkController(SchemaService schemaService, ApiLinkService linkService, DatasetService datasetService)
    {
        _schemaService = schemaService;
        _linkService = linkService;
        _datasetService = datasetService;
    }

    [HttpPost]
    public IActionResult Create(Guid schemaId, [FromBody] CreateApiLinkRequest request)
    {
        if (!ApiLinkService.ValidCount(request.DefaultRecordCount) || !ValidExpiry(request.ExpiresAt))
            return BadRequest(new { error = "Record count must be between 1 and 1000 and expiry must be in the future." });
        var (link, key) = _linkService.Create(schemaId, request.DefaultRecordCount, request.ExpiresAt);
        if (link is null || key is null) return NotFound();
        var response = CreatedResponse(link, key);
        return Created($"/api/schema/{schemaId}/links/{link.Id}", response);
    }

    [HttpGet]
    public async Task<IActionResult> List(Guid schemaId)
    {
        if (await _schemaService.GetSchemaAsync(schemaId) is null) return NotFound();
        return Ok(_linkService.GetBySchema(schemaId).Select(ToResponse));
    }

    [HttpPut("{linkId:guid}")]
    public async Task<IActionResult> Update(Guid schemaId, Guid linkId, [FromBody] UpdateApiLinkRequest request)
    {
        if (!ApiLinkService.ValidCount(request.DefaultRecordCount) || !ValidExpiry(request.ExpiresAt))
            return BadRequest(new { error = "Record count must be between 1 and 1000 and expiry must be in the future." });
        if (await _schemaService.GetSchemaAsync(schemaId) is null) return NotFound();
        var link = _linkService.Update(schemaId, linkId, request.DefaultRecordCount, request.ExpiresAt);
        return link is null ? NotFound() : Ok(ToResponse(link));
    }

    [HttpDelete("{linkId:guid}")]
    public IActionResult Revoke(Guid schemaId, Guid linkId) =>
        _linkService.Revoke(schemaId, linkId) ? NoContent() : NotFound();

    [HttpPost("{linkId:guid}/rotate")]
    public IActionResult Rotate(Guid schemaId, Guid linkId)
    {
        var (link, key) = _linkService.Rotate(schemaId, linkId);
        return link is null || key is null ? NotFound() : Ok(CreatedResponse(link, key));
    }

    [HttpPost("{linkId:guid}/regenerate")]
    public async Task<IActionResult> Regenerate(Guid schemaId, Guid linkId, CancellationToken cancellationToken)
    {
        var link = _linkService.GetActive(schemaId, linkId);
        if (link is null) return NotFound();
        try
        {
            var dataset = await _datasetService.GetOrCreateAsync(link, link.DefaultRecordCount, regenerate: true, cancellationToken: cancellationToken);
            return dataset is null ? NotFound() : Ok(dataset);
        }
        catch (NotSupportedException exception)
        {
            return BadRequest(new { error = exception.Message });
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or InvalidDataException or System.Text.Json.JsonException)
        {
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new { error = "The dataset could not be stored or loaded. Check the configured JSON storage and retry." });
        }
    }

    private static ApiLinkResponse ToResponse(Data.Entities.ApiLink link) => new(
        link.Id, link.SchemaId, link.IsActive, DateTime.SpecifyKind(link.CreatedAt, DateTimeKind.Utc),
        link.ExpiresAt.HasValue ? DateTime.SpecifyKind(link.ExpiresAt.Value, DateTimeKind.Utc) : null,
        link.DefaultRecordCount);

    private CreatedApiLinkResponse CreatedResponse(Data.Entities.ApiLink link, string key) => new(
        link.Id, link.SchemaId, $"{Request.Scheme}://{Request.Host}/api/v1/data/{key}",
        link.IsActive, DateTime.SpecifyKind(link.CreatedAt, DateTimeKind.Utc),
        link.ExpiresAt.HasValue ? DateTime.SpecifyKind(link.ExpiresAt.Value, DateTimeKind.Utc) : null,
        link.DefaultRecordCount);

    private static bool ValidExpiry(DateTime? expiry) =>
        !expiry.HasValue || DateTime.SpecifyKind(expiry.Value, DateTimeKind.Utc) > DateTime.UtcNow;
}
