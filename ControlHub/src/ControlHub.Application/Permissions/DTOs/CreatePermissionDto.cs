using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ControlHub.Application.Permissions.DTOs
{
    public sealed record CreatePermissionDto(
        string Code,
        string? Description
        );
}
