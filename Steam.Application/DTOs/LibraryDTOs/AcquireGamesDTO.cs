namespace Steam.Application.DTOs.LibraryDTOs;

public class AcquireGamesDTO
{
    public List<Guid> GameIds { get; set; } = [];
    public bool DemoPurchase { get; set; }
}
