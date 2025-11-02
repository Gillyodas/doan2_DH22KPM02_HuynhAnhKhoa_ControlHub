using AutoMapper;
using ControlHub.Application.Roles.DTOs;
using ControlHub.Domain.Roles;

namespace ControlHub.Application.Roles.MappingProfiles
{
    public class RolePermissionQueryProfile : Profile
    {
        public RolePermissionQueryProfile()
        {
            CreateMap<RolePermission, RolePermissionDetailDto>()
                .ForMember(dest => dest.RoleName, opt => opt.Ignore())
                .ForMember(dest => dest.PermissionName, opt => opt.Ignore());
        }
    }
}