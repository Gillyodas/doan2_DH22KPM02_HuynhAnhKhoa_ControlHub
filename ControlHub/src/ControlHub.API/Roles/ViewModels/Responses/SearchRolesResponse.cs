using ControlHub.Application.Common.DTOs;
using ControlHub.Domain.Roles;

namespace ControlHub.API.Roles.ViewModels.Responses
{
    public class SearchRolesResponse
    {
        public PagedResult<Role> Result { get; set; } = null!;
        public string Message { get; set; } = null!;
    }
}
