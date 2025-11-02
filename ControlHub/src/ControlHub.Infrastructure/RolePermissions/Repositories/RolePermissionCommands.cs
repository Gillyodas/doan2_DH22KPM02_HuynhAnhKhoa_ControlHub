using AutoMapper;
using ControlHub.Application.Roles.Interfaces.Repositories;
using ControlHub.Domain.Roles;
using ControlHub.Infrastructure.Persistence;

namespace ControlHub.Infrastructure.RolePermissions.Repositories
{
    public class RolePermissionCommands : IRolePermissionsCommands
    {
        private readonly AppDbContext _context;
        private readonly IMapper _mapper;
        public RolePermissionCommands(AppDbContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }
    }
}
