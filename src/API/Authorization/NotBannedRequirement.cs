using Microsoft.AspNetCore.Authorization;

namespace API.Authorization;

public class NotBannedRequirement : IAuthorizationRequirement
{
}
