using AutoMapper;
using ControlHub.Application.Roles.DTOs;
using ControlHub.Domain.Roles;

namespace ControlHub.Application.Roles.Commands.MappingProfiles
{
    public class RolePermissionCommandProfile : Profile
    {
        public RolePermissionCommandProfile()
        {
            CreateMap<RolePermission, RolePermissionDto>().ReverseMap();
        }
    }
}