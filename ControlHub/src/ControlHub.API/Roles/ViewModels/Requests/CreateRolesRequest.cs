using ControlHub.Application.Roles.DTOs;

namespace ControlHub.API.Roles.ViewModels.Requests
{
    public class CreateRolesRequest
    {
        public IEnumerable<CreateRoleDto> Roles { get; set; } = null!;
    }
}
