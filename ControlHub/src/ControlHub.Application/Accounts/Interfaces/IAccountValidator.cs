using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ControlHub.Domain.Accounts.ValueObjects;
using ControlHub.SharedKernel.Results;

namespace ControlHub.Application.Accounts.Interfaces
{
    public interface IAccountValidator
    {
        Task<Result<bool>> EmailIsExistAsync(Email email);
    }
}
