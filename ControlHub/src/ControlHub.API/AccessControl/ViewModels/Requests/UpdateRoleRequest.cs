namespace ControlHub.API.AccessControl.ViewModels.Requests
{
    public record UpdateRoleRequest(
        string Name,
        string Description
    );
}
