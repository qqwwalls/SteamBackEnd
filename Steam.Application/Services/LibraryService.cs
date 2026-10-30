using Steam.Application.DTOs.LibraryDTOs;
using Steam.Application.Exceptions;
using Steam.Application.Interfaces.Repository;
using Steam.Application.Interfaces.Services;
using Steam.Domain.Model;
using Steam.Application.Configuration;
using Steam.Application.Mapping;
using Microsoft.Extensions.Options;

namespace Steam.Application.Services;

public class LibraryService : ILibraryService
{
    private readonly ILibraryRepository _library;
    private readonly IGameRepository _games;
    private readonly ICurrentUserService _currentUser;
    private readonly CheckoutSettings _checkoutSettings;

    public LibraryService(ILibraryRepository library, IGameRepository games, ICurrentUserService currentUser,
        IOptions<CheckoutSettings> checkoutSettings)
    {
        _library = library;
        _games = games;
        _currentUser = currentUser;
        _checkoutSettings = checkoutSettings.Value;
    }

    public async Task<IEnumerable<LibraryGameDTO>> GetAsync(CancellationToken ct)
    {
        var entries = await _library.GetByUserIdAsync(_currentUser.UserId, ct);
        return entries.Select(item => new LibraryGameDTO
        {
            Id = item.Game.Id,
            Title = item.Game.Name,
            Price = item.Game.Price,
            CoverImageUrl = GameArtwork.GetCover(item.Game),
            BannerImageUrl = GameArtwork.GetBanner(item.Game),
            ReleaseDate = item.Game.ReleaseDate,
            PurchasedAt = item.PurchasedAt,
            Playtime = item.Playtime
        }).ToList();
    }

    public async Task<IEnumerable<LibraryGameDTO>> AcquireAsync(AcquireGamesDTO dto, CancellationToken ct)
    {
        var userId = _currentUser.UserId;
        var ids = dto.GameIds.Distinct().ToArray();
        var games = await _games.GetByIdsAsync(ids, ct);
        if (games.Count != ids.Length)
            throw new KeyNotFoundException("One or more games were not found.");
        if (games.Any(game => game.ReleaseDate > DateTime.UtcNow))
            throw new ConflictException("One or more games have not been released yet.");

        // Paid games are granted only by an explicitly enabled demo checkout.
        if (games.Any(game => game.Price != 0))
        {
            if (!_checkoutSettings.EnableDemoPurchases)
                throw new PaymentUnavailableException();
            if (!dto.DemoPurchase)
                throw new ArgumentException("Confirm the demo purchase. No money will be charged.");
        }

        var owned = await _library.GetByUserIdAsync(userId, ct);
        if (owned.Any(item => ids.Contains(item.GameId)))
            throw new ConflictException("One or more games are already in your library.");

        await _library.AddAsync(ids.Select(id => new UserGame
        {
            UserId = userId, GameId = id, PurchasedAt = DateTime.UtcNow, Playtime = 0
        }), ct);
        return await GetAsync(ct);
    }
}
