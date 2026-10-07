using Daas.Api.Services;
using Daas.Api.Contracts;
using Daas.Domain.Entities;
using Microsoft.AspNetCore.Mvc;
using System.Collections;

namespace Daas.API.Controllers;

[ApiController]
[Route("api/users")]
public class UsersController : ControllerBase
{
    private readonly UserService _userService;

    public UsersController(UserService userService)
    {
        _userService = userService;
    }

    [HttpGet]
    public async Task<IActionResult> Get()
    {
        var users = await _userService.GetUsers();
        return Ok(users);
    }

    [Route("/akash")]
    [HttpGet]
    public Task<Dummy> dummy()
    {
        return _userService.GetDummy();
    }

    [Route("/DummyData/{howmany}")]
    [HttpPost]
    public Task<ArrayList> DummyDataGenerator([FromBody] RequestModel[] sus, [FromRoute] int howmany)
    {
        return _userService.GeneratePayload(sus, howmany);
    }
}
