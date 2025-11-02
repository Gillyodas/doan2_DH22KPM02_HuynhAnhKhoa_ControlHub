using ControlHub.Application.Permissions.DTOs;

namespace ControlHub.API.Permissions.ViewModels.Requests
{
    public class CreatePermissionsRequest
    {
        public IEnumerable<CreatePermissionDto> Permissions { get; set; } = null!;
    }
}
