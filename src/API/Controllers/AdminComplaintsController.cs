using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Users.Application.DTOs;
using Users.Application.Interfaces;

namespace API.Controllers;

[ApiController]
[Route("api/admin/complaints")]
[Authorize(Roles = "ADMIN")]
public class AdminComplaintsController : ControllerBase
{
    private readonly IUserService _userService;

    public AdminComplaintsController(IUserService userService)
    {
        _userService = userService;
    }

    [HttpGet("{id}")]
    [ProducesResponseType(typeof(ComplaintResponseDTO), 200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetComplaintById([FromRoute] Guid id)
    {
        var result = await _userService.GetComplaintById(id);
        return Ok(result);
    }
}
