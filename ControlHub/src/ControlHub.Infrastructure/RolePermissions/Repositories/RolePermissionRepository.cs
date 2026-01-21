using AutoMapper;
using ControlHub.Application.Roles.Interfaces.Repositories;
using ControlHub.Infrastructure.Persistence;

namespace ControlHub.Infrastructure.RolePermissions.Repositories
{
    internal class RolePermissionRepository : IRolePermissionsRepository
    {
        private readonly AppDbContext _context;
        private readonly IMapper _mapper;
        public RolePermissionRepository(AppDbContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }
    }
}
