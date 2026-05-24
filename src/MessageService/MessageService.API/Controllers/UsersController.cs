using MessageService.Infrastructure.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace MessageService.API.Controllers;

[ApiController]
[Route("api/users")]
public class UsersController : ControllerBase
{
    private readonly IUserRepository _users;
    public UsersController(IUserRepository users) => _users = users;

    [HttpGet("{userId}")]
    public async Task<IActionResult> Get(System.Guid userId)
    {
        var u = await _users.GetByIdAsync(userId);
        if (u == null) return NotFound();
        return Ok(new { id = u.Id, username = u.Username, displayName = u.DisplayName });
    }
}

