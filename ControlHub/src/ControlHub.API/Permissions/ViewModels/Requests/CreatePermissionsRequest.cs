using ControlHub.Application.AccessControl.DTOs;

namespace ControlHub.API.Permissions.ViewModels.Requests
{
    public class CreatePermissionsRequest
    {
        public IEnumerable<CreatePermissionDto> Permissions { get; set; } = null!;
    }
}
