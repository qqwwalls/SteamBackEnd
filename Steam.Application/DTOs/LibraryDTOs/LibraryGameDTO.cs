using Steam.Application.DTOs.GameDTOs;

namespace Steam.Application.DTOs.LibraryDTOs;

public class LibraryGameDTO : GameListItemDTO
{
    public DateTime PurchasedAt { get; set; }
    public int Playtime { get; set; }
}
