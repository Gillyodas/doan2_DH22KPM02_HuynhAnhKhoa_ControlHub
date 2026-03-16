using AutoMapper;
using ControlHub.Infrastructure.Persistence;

namespace ControlHub.Infrastructure.AccessControl.Persistence.Repositories
{
    internal class RolePermissionQuery
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
