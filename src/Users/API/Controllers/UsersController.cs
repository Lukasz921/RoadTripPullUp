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

    [HttpPost("{id}/rating")]
    public async Task<IActionResult> RateUser(Guid id, [FromBody] int value, [FromQuery] string? comment)
    {
        var raterIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
        if (raterIdClaim == null) return Unauthorized();

        var raterId = Guid.Parse(raterIdClaim.Value);
        
        await _userService.AddRating(new AddRatingDTO
        {
            UserId = id,
            RaterId = raterId,
            Value = value,
            Comment = comment
        });

        return Ok();
    }

    [HttpGet("{id}/ratings")]
    [AllowAnonymous]
    public async Task<IActionResult> GetRatings(Guid id)
    {
        var ratings = await _userService.GetUserRatings(id);
        return Ok(ratings);
    }

    [HttpGet("ratings/{ratingId}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetRating(Guid ratingId)
    {
        var rating = await _userService.GetRating(ratingId);
        return Ok(rating);
    }

    [HttpDelete("ratings/{ratingId}")]
    public async Task<IActionResult> DeleteRating(Guid ratingId)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
        if (userIdClaim == null) return Unauthorized();

        var userId = Guid.Parse(userIdClaim.Value);
        await _userService.DeleteRating(ratingId, userId);
        return NoContent();
    }
}
