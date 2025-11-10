<<<<<<< Updated upstream
﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ControlHub.Domain.Roles;
=======
﻿using ControlHub.Domain.Roles;
>>>>>>> Stashed changes

namespace ControlHub.Application.Roles.Interfaces.Repositories
{
    public interface IRoleQueries
    {
        Task<Role> GetByIdAsync(Guid id, CancellationToken cancellationToken);
        Task<IEnumerable<Role>> GetAllAsync(CancellationToken cancellationToken);
        Task<IEnumerable<Role>> SearchByNameAsync(string name, CancellationToken cancellationToken);
        Task<bool> ExistAsync(Guid roleId, CancellationToken cancellationToken);
    }
}
