using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Users.Application.DTOs;
using Users.Application.Interfaces;
using Users.Core;

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

    [HttpGet("{id}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetById(Guid id)
    {
        var user = await _userService.GetById(id);
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

    [HttpPost("{id}/ban")]
    [Authorize(Roles = "ADMIN")]
    public async Task<IActionResult> Ban(Guid id, [FromBody] BanUserDTO dto)
    {
        await _userService.Ban(id, dto);
        return Ok();
    }

    [HttpPost("{id}/unban")]
    [Authorize(Roles = "ADMIN")]
    public async Task<IActionResult> Unban(Guid id)
    {
        await _userService.Unban(id);
        return Ok();
    }

    [HttpPost("{id}/role")]
    [Authorize(Roles = "ADMIN")]
    public async Task<IActionResult> ChangeRole(Guid id, [FromBody] UserRole role)
    {
        await _userService.ChangeRole(id, role);
        return Ok();
    }
}
