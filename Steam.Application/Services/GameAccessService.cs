using Steam.Application.Interfaces.Repository;
using Steam.Application.Interfaces.Services;
using Steam.Domain.Model;

namespace Steam.Application.Services;

public class GameAccessService : IGameAccessService
{
    private readonly IGameRepository _games;
    private readonly ICurrentUserService _currentUser;

    public GameAccessService(IGameRepository games, ICurrentUserService currentUser)
    {
        _games = games;
        _currentUser = currentUser;
    }

    public async Task<Game> GetWritableAsync(Guid gameId, CancellationToken ct)
    {
        var game = await _games.GetByIdAsync(gameId, ct)
            ?? throw new KeyNotFoundException("Game was not found.");
        if (!_currentUser.IsAdmin && game.DeveloperId != _currentUser.UserId)
            throw new UnauthorizedAccessException();
        return game;
    }
}
