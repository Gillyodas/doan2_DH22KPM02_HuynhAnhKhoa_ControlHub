using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ControlHub.Domain.Accounts.Enums;
using ControlHub.Domain.Accounts.ValueObjects;
using ControlHub.SharedKernel.Results;

namespace ControlHub.Application.Accounts.Interfaces
{
    public interface IAccountValidator
    {
        Task<bool> IdentifierIsExist(string Value, IdentifierType Type, CancellationToken cancellationToken);
    }
}
