using AutoMapper;
using WebApplicationShop.Dto;
using WebApplicationShop.Models;

namespace WebApplicationShop.Mapping
{
    public class AppMappingProfile : Profile
    {
        public AppMappingProfile()
        {
           CreateMap<RegisterDto, User>()
                .ForMember(dest => dest.Email, opt => opt.MapFrom(src => src.Email.Trim()))
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.PasswordHash, opt => opt.Ignore()) 
                .ForMember(dest => dest.RoleId, opt => opt.Ignore())       
                .ForMember(dest => dest.Role, opt => opt.Ignore());

            CreateMap<User, UserDto>()
                .ForMember(dest => dest.Name, opt => opt.MapFrom(src => src.Email.Substring(0, src.Email.IndexOf("@"))))
                .ForMember(dest => dest.RoleName, opt => opt.MapFrom(src => src.Role != null ? src.Role.Name : "без роли"));
        }
    }
}