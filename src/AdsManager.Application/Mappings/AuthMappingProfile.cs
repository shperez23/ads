using AdsManager.Application.DTOs.Auth;
using AdsManager.Domain.Entities;
using AutoMapper;

namespace AdsManager.Application.Mappings;

public sealed class AuthMappingProfile : Profile
{
    public AuthMappingProfile()
    {
        CreateMap<User, UserProfileDto>()
            .ForCtorParam(nameof(UserProfileDto.Role), opt => opt.MapFrom(src => src.Role.Name));
    }
}
