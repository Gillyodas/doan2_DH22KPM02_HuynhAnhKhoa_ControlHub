using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ControlHub.Application.Accounts.DTOs
{
    public record IdentifierConfigDto(
        Guid Id,
        string Name,
        string Description,
        bool IsActive,
        List<ValidationRuleDto> Rules
    );
}
