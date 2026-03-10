using ControlHub.Application.AccessControl.DTOs;

namespace ControlHub.API.AccessControl.ViewModels.Requests
{
    public class CreatePermissionsRequest
    {
        public IEnumerable<CreatePermissionDto> Permissions { get; set; } = null!;
    }
}
