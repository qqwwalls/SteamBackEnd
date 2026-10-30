using AutoMapper;
using Steam.Domain.Model;
using Steam.Application.DTOs.AuthDTOs;
using Steam.Application.DTOs;
using Steam.Application.DTOs.UserDTOs;
using Steam.Application.DTOs.GameDTOs;
using Steam.Application.DTOs.GenreDTOs;
using Steam.Application.DTOs.TagDTOs;
using Steam.Application.DTOs.ProviderDTOs;
using Steam.Application.DTOs.MediaDTOs;
using Steam.Application.DTOs.SystemRequirementsDTOs;
using Steam.Application.DTOs.AchievementDTOs;

namespace Steam.Application.Mapping;

public class SteamProfile : Profile
{
    public SteamProfile()
    {
        CreateMap<UserCreateDTO, User>();
        CreateMap<User, UserDTO>()
            .ForMember(dest => dest.AvatarUrl, opt => opt.MapFrom(src => src.Avatar == null ? null : src.Avatar.Url));

        CreateMap<GameCreateDTO, Game>();
        CreateMap<GameUpdateDTO, Game>()
            .ForMember(dest => dest.Name, opt => opt.MapFrom(src => src.Title));
        CreateMap<Game, GameDTO>()
            .ForMember(dest => dest.Title, opt => opt.MapFrom(src => src.Name))
            .ForMember(dest => dest.DeveloperName, opt => opt.MapFrom(src => src.Developer.Username))
            .ForMember(dest => dest.CoverImageUrl, opt => opt.MapFrom(src => GameArtwork.GetCover(src)))
            .ForMember(dest => dest.BannerImageUrl, opt => opt.MapFrom(src => GameArtwork.GetBanner(src)));

        CreateMap<Game, GameListItemDTO>()
            .ForMember(dest => dest.Title, opt => opt.MapFrom(src => src.Name))
            .ForMember(dest => dest.CoverImageUrl, opt => opt.MapFrom(src => GameArtwork.GetCover(src)))
            .ForMember(dest => dest.BannerImageUrl, opt => opt.MapFrom(src => GameArtwork.GetBanner(src)));

        CreateMap<Genre, GenreDTO>();
        CreateMap<Tag, TagDTO>();
        CreateMap<Provider, ProviderDTO>();

        CreateMap<MediaCreateDTO, Media>();
        CreateMap<Media, MediaDTO>();

        CreateMap<SystemRequirementsCreateDTO, SystemRequirements>()
            .ForMember(dest => dest.Processor, opt => opt.MapFrom(src => src.Cpu))
            .ForMember(dest => dest.Graphics, opt => opt.MapFrom(src => src.Gpu))
            .ForMember(dest => dest.Memory, opt => opt.MapFrom(src => src.Ram));
        CreateMap<SystemRequirements, SystemRequirementsDTO>()
            .ForMember(dest => dest.Cpu, opt => opt.MapFrom(src => src.Processor))
            .ForMember(dest => dest.Gpu, opt => opt.MapFrom(src => src.Graphics))
            .ForMember(dest => dest.Ram, opt => opt.MapFrom(src => src.Memory));

        CreateMap<AchievementCreateDTO, Achievement>()
            .ForMember(dest => dest.Name, opt => opt.MapFrom(src => src.Title));
        CreateMap<Achievement, AchievementDTO>()
            .ForMember(dest => dest.Title, opt => opt.MapFrom(src => src.Name));
    }
}
