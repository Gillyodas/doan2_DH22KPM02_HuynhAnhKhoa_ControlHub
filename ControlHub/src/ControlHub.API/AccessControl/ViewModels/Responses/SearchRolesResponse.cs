using ControlHub.Domain.AccessControl.Aggregates;
using ControlHub.SharedKernel.Common.DTOs;

namespace ControlHub.API.AccessControl.ViewModels.Responses
{
    public class SearchRolesResponse
    {
        public PagedResult<Role> Result { get; set; } = null!;
        public string Message { get; set; } = null!;
    }
}
