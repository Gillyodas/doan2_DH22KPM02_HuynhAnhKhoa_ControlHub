using AutoMapper;
using ControlHub.Application.Roles.Interfaces.Repositories;
using ControlHub.Infrastructure.Persistence;

namespace ControlHub.Infrastructure.RolePermissions.Repositories
{
    internal class RolePermissionQuery : IRolePermissionQueries
    {
        private readonly AppDbContext _db;
        private readonly IMapper _mapper;
        public RolePermissionQuery(AppDbContext db, IMapper mapper)
        {
            _db = db;
            _mapper = mapper;
        }
    }
}
