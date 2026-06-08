using Users.Application.Interfaces;
using Users.Application.DTOs;
using Microsoft.AspNetCore.Mvc;

namespace Users.API.Controllers;

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
        var response = await _authService.Login(dto);
        return Ok(response);
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterDTO dto)
    {
        await _authService.Register(dto);
        return Ok();
    }

    [HttpPost("google")]
    public async Task<IActionResult> Google([FromBody] GoogleLoginDTO dto)
    {
        var response = await _authService.GoogleLogin(dto.IdToken);
        return Ok(response);
    }
}
