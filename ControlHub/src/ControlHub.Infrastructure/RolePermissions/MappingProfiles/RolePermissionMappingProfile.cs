using AutoMapper;
using ControlHub.Domain.Roles;

namespace ControlHub.Infrastructure.RolePermissions
{
    public class RolePermissionMappingProfile : Profile
    {
        public RolePermissionMappingProfile()
        {
            CreateMap<RolePermissionEntity, RolePermission>().ReverseMap();
        }
    }
}