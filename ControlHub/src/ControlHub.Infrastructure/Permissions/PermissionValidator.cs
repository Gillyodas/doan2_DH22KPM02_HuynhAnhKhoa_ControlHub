using ControlHub.Application.Roles.Interfaces.Repositories;
using ControlHub.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ControlHub.Infrastructure.Permissions
{
    internal class PermissionValidator : IPermissionValidator
    {
        private readonly AppDbContext _db;
        private readonly ILogger<PermissionValidator> _looger;

        public PermissionValidator(AppDbContext db, ILogger<PermissionValidator> logger)
        {
            _db = db;
            _looger = logger;
        }
        public async Task<List<Guid>> PermissionIdsExistAsync(IEnumerable<Guid> permissionIds, CancellationToken cancellationToken)
        {
            return await _db.Permissions
            .Where(p => permissionIds.Contains(p.Id))
            .Select(p => p.Id)
            .ToListAsync(cancellationToken);
        }
    }
}
