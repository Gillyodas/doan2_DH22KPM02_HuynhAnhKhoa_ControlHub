using ControlHub.Application.Common.DTOs;
using ControlHub.Domain.AccessControl.Aggregates;

namespace ControlHub.API.Roles.ViewModels.Responses
{
    public class SearchRolesResponse
    {
        public PagedResult<Role> Result { get; set; } = null!;
        public string Message { get; set; } = null!;
    }
}
