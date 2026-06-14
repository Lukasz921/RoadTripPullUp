using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Users.Application.Interfaces;

namespace API.Authorization;

public class NotBannedHandler : AuthorizationHandler<NotBannedRequirement>
{
    private readonly IUserService _userService;

    public NotBannedHandler(IUserService userService)
    {
        _userService = userService;
    }

    protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, NotBannedRequirement requirement)
    {
        var userIdClaim = context.User.FindFirst(ClaimTypes.NameIdentifier);
        if (userIdClaim == null)
        {
            return;
        }

        if (!Guid.TryParse(userIdClaim.Value, out var userId))
        {
            return;
        }

        try
        {
            var userResponse = await _userService.GetById(userId);
            
            if (!userResponse.IsCurrentlyBanned())
            {
                context.Succeed(requirement);
            }
        }
        catch
        {
            // If user not found, we don't succeed.
        }
    }
}
