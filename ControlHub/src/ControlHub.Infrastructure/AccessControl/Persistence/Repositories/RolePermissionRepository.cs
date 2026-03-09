using AutoMapper;
using ControlHub.Application.AccessControl.Interfaces.Repositories;
using ControlHub.Infrastructure.Persistence;

namespace ControlHub.Infrastructure.AccessControl.Persistence.Repositories
{
    internal class RolePermissionRepository
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
