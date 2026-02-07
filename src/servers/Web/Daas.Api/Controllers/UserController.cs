using Daas.Application.Users.Queries;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Daas.Domain.Entities;
using Daas.Application.DTO.RequestDTO;
using System.Collections;
namespace Daas.API.Controllers;

[ApiController]
[Route("api/users")]
public class UsersController : ControllerBase
{
    private readonly IMediator _mediator;

    public UsersController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    public async Task<IActionResult> Get()
    {
        var users = await _mediator.Send(new GetUsersQuery());
        return Ok(users);
    }
    [Route("/akash")]
    [HttpGet]
    public Task<Dummy> dummy()
    {
       var dummies = _mediator.Send(new GetDummyQuery());
       return dummies;
    }
    [Route("/DummyData/{howmany}")]
    [HttpPost]
    public Task<ArrayList> DummyDataGenerator([FromBody] RequestModel[] sus, [FromRoute]int howmany)
    {
        var result = _mediator.Send(new GetPayloadQuery(sus,howmany));
        return result;
    }
}
