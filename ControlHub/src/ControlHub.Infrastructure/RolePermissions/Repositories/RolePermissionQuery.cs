using AutoMapper;
using ControlHub.Application.Roles.DTOs;
using ControlHub.Application.Roles.Interfaces.Repositories;
using ControlHub.Domain.Roles;
using ControlHub.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ControlHub.Infrastructure.RolePermissions.Repositories
{
    public class RolePermissionQuery : IRolePermissionQuery
    {
        private readonly AppDbContext _db;
        private readonly IMapper _mapper;
        public RolePermissionQuery(AppDbContext db, IMapper mapper)
        {
            _db = db;
            _mapper = mapper;
        }
        public async Task<IEnumerable<RolePermission>> GetAllAsync(CancellationToken cancellationToken)
        {
            var entities = await _db.RolePermissions
                .AsNoTracking()
                .ToListAsync(cancellationToken);

            return _mapper.Map<IEnumerable<RolePermission>>(entities);
        }

        public async Task<IEnumerable<RolePermissionDetailDto>> GetAllWithNameAsync(CancellationToken cancellationToken)
        {
            var result = await _db.RolePermissions
                .AsNoTracking()
                .Include(rp => rp.Role)
                .Include(rp => rp.Permission)
                .Select(rp => new RolePermissionDetailDto(
                    rp.RoleId,
                    rp.Role.Name,
                    rp.PermissionId,
                    rp.Permission.Code
                ))
                .ToListAsync(cancellationToken);

            return result;
        }
    }
}
