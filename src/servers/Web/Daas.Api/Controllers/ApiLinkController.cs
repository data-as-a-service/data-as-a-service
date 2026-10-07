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

    public ApiLinkController(SchemaService schemaService, ApiLinkService linkService)
    {
        _schemaService = schemaService;
        _linkService = linkService;
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
    public IActionResult List(Guid schemaId)
    {
        if (_schemaService.GetSchema(schemaId) is null) return NotFound();
        return Ok(_linkService.GetBySchema(schemaId).Select(ToResponse));
    }

    [HttpPut("{linkId:guid}")]
    public IActionResult Update(Guid schemaId, Guid linkId, [FromBody] UpdateApiLinkRequest request)
    {
        if (!ApiLinkService.ValidCount(request.DefaultRecordCount) || !ValidExpiry(request.ExpiresAt))
            return BadRequest(new { error = "Record count must be between 1 and 1000 and expiry must be in the future." });
        if (_schemaService.GetSchema(schemaId) is null) return NotFound();
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

    private static ApiLinkResponse ToResponse(Data.Entities.ApiLink link) => new(
        link.Id, link.SchemaId, link.IsActive, DateTime.SpecifyKind(link.CreatedAt, DateTimeKind.Utc),
        link.ExpiresAt.HasValue ? DateTime.SpecifyKind(link.ExpiresAt.Value, DateTimeKind.Utc) : null,
        link.DefaultRecordCount);

    private CreatedApiLinkResponse CreatedResponse(Data.Entities.ApiLink link, string key) => new(
        link.Id, link.SchemaId, $"{Request.Scheme}://{Request.Host}/api/mock/{key}",
        link.IsActive, DateTime.SpecifyKind(link.CreatedAt, DateTimeKind.Utc),
        link.ExpiresAt.HasValue ? DateTime.SpecifyKind(link.ExpiresAt.Value, DateTimeKind.Utc) : null,
        link.DefaultRecordCount);

    private static bool ValidExpiry(DateTime? expiry) =>
        !expiry.HasValue || DateTime.SpecifyKind(expiry.Value, DateTimeKind.Utc) > DateTime.UtcNow;
}
