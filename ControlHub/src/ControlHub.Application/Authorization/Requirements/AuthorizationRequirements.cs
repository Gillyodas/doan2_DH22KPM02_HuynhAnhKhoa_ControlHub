using Microsoft.AspNetCore.Authorization;

namespace ControlHub.Application.Authorization.Requirements
{
    public class SameUserRequirement : IAuthorizationRequirement
    {
    }

    public class PermissionRequirement : IAuthorizationRequirement
    {
        public string Permission { get; }

        public PermissionRequirement(string permission)
        {
            Permission = permission;
        }
    }
}
