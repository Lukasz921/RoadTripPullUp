using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Users.Application.DTOs;
using Users.Application.Interfaces;

namespace Users.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class UsersController : ControllerBase
{
    private readonly IUserService _userService;

    public UsersController(IUserService userService)
    {
        _userService = userService;
    }

    [HttpGet("me")]
    public async Task<IActionResult> GetCurrent()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
        if (userIdClaim == null) return Unauthorized();

        var userId = Guid.Parse(userIdClaim.Value);
        var user = await _userService.GetById(userId);
        return Ok(user);
    }

    [HttpPatch("me")]
    public async Task<IActionResult> UpdateCurrent([FromBody] UpdateUserDTO dto)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
        if (userIdClaim == null) return Unauthorized();

        var userId = Guid.Parse(userIdClaim.Value);
        await _userService.Update(userId, dto);
        return NoContent();
    }

    [HttpGet("me/integration-data")]
    public async Task<IActionResult> GetIntegrationData()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
        if (userIdClaim == null) return Unauthorized();

        var userId = Guid.Parse(userIdClaim.Value);
        var data = await _userService.GetUserIntegrationData(userId);
        return Ok(data);
    }
}
