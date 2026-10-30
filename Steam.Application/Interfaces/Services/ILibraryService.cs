using Steam.Application.DTOs.LibraryDTOs;

namespace Steam.Application.Interfaces.Services;

public interface ILibraryService
{
    Task<IEnumerable<LibraryGameDTO>> GetAsync(CancellationToken ct);
    Task<IEnumerable<LibraryGameDTO>> AcquireAsync(AcquireGamesDTO dto, CancellationToken ct);
}
