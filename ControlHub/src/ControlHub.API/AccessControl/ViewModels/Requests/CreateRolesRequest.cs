using ControlHub.Application.AccessControl.DTOs;

namespace ControlHub.API.AccessControl.ViewModels.Requests
{
    public class CreateRolesRequest
    {
        public IEnumerable<CreateRoleDto> Roles { get; set; } = null!;
    }
}
