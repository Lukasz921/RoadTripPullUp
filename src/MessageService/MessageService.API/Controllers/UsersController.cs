using Application.Exceptions;
using MessageService.Core.RepositoryInterfaces;
using Microsoft.AspNetCore.Mvc;

namespace MessageService.API.Controllers;

[ApiController]
[Route("api/v1/message/users")]
public class UsersController : ControllerBase
{
    private readonly IUserRepository _users;
    public UsersController(IUserRepository users) => _users = users;

    [HttpGet("{userId:guid}")]
    public async Task<IActionResult> Get(Guid userId)
    {
        var u = await _users.GetByIdAsync(userId);
        return u == null ? throw new NotFoundException("User not found") : Ok(new { id = u.Id, username = u.Username, displayName = u.DisplayName });
        // TODO: refactor the return value
    }
}

