using Steam.Domain.Enum;
using Steam.Domain.Model;

namespace Steam.Application.Mapping;

public static class GameArtwork
{
    public const string CoverFileName = "library_600x900_2x.jpg";
    public const string BannerFileName = "library_hero.jpg";

    public static string? GetCover(Game game) => game.Media.Where(media => media.Type == MediaType.Image)
        .OrderBy(media => media.FileName == CoverFileName ? 0 : 1).ThenBy(media => media.Id)
        .Select(media => media.Url).FirstOrDefault();

    public static string? GetBanner(Game game) => game.Media.Where(media => media.Type == MediaType.Image)
        .OrderBy(media => media.FileName == BannerFileName ? 0 : 1).ThenBy(media => media.Id)
        .Select(media => media.Url).FirstOrDefault();
}
