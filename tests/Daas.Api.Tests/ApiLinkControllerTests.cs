using Daas.Api.Controllers;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace Daas.Api.Tests;

public sealed class ApiLinkControllerTests
{
    [Theory]
    [InlineData(-1)]
    [InlineData(2)]
    public async Task RegenerateRejectsUnsupportedGenerationMode(int generationMode)
    {
        var controller = new ApiLinkController(null!, null!, null!);

        var result = await controller.Regenerate(Guid.NewGuid(), Guid.NewGuid(), generationMode, CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(result);
    }
}
