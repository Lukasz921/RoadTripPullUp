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

    [HttpGet]
    [ProducesResponseType(typeof(PagedComplaintsDTO), 200)]
    public async Task<IActionResult> GetAllComplaints(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        pageSize = Math.Clamp(pageSize, 1, 100);
        page = Math.Max(page, 1);

        var result = await _userService.GetAllComplaints(page, pageSize);
        return Ok(result);
    }

    [HttpDelete("{id}")]
    [ProducesResponseType(204)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> DeleteComplaint([FromRoute] Guid id)
    {
        await _userService.DeleteComplaint(id);
        return NoContent();
    }
}
