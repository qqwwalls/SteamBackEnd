using Steam.Domain.Model;

namespace Steam.Application.Interfaces.Services;

public interface IGameAccessService
{
    Task<Game> GetWritableAsync(Guid gameId, CancellationToken ct);
}
