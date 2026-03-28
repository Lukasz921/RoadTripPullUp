using Application.DTOs;
using Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginDTO dto)
    {
        try
        {
            var response = await _authService.Login(dto);
            return Ok(response);
        }
        catch (Exception ex)
        {
            return Unauthorized(new { message = ex.Message });
        }
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] UserDTO dto)
    {
        try
        {
            await _authService.Register(dto);
            return Ok();
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("google")]
    public async Task<IActionResult> Google([FromBody] GoogleLoginDTO dto)
    {
        try
        {
            var response = await _authService.GoogleLogin(dto.IdToken);
            return Ok(response);
        }
        catch (Exception ex)
        {
            return Unauthorized(new { message = "Invalid Google token.", details = ex.Message });
        }
    }
}