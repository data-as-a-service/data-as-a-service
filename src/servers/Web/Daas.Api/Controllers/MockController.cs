using Daas.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace Daas.Api.Controllers;

[ApiController]
[Route("mock")]
public class MockController : ControllerBase
{
    private readonly SchemaService _schemaService;

    public MockController(SchemaService schemaService)
    {
        _schemaService = schemaService;
    }

    [HttpGet("{id}")]
    public IActionResult GetMockData(Guid id)
    {
        var result = _schemaService.GenerateMockData(id);
        if (result == null)
        {
            return NotFound();
        }

        return Ok(result);
    }
}
