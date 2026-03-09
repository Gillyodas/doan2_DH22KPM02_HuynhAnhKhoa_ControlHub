using Microsoft.AspNetCore.Authorization;

namespace ControlHub.Application.Identity.Authorization
{
    public class SameUserRequirement : IAuthorizationRequirement
    {
    }
}
