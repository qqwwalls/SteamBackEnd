using Steam.Application.DTOs.GameDTOs;
using Steam.Application.Interfaces.Repository;
using Steam.Application.Mapping;
using Steam.Application.Interfaces.Services;
using Steam.Domain.Model;
using System;
using System.Collections.Generic;
using System.Text;

namespace Steam.Application.Services;

public class WishlistService : IWishlistService
{
    private readonly IWishlistRepository _repository;
    private readonly ICurrentUserService _currentUser;
    private readonly IGameRepository _games;

    public WishlistService(
        IWishlistRepository repository,
        ICurrentUserService currentUser,
        IGameRepository games)
    {
        _repository = repository;
        _currentUser = currentUser;
        _games = games;
    }
    public async Task<IEnumerable<GameListItemDTO>> GetWishlistAsync()
    {
        var userId = _currentUser.UserId;
        var wishlist = await _repository.GetByUserIdAsync(userId);
        var gameDTOs = new List<GameListItemDTO>();
        foreach (var item in wishlist)
        {
            gameDTOs.Add(new GameListItemDTO
            {
                Id = item.Game.Id,
                Title = item.Game.Name,
                Price = item.Game.Price,
                ReleaseDate = item.Game.ReleaseDate,
                CoverImageUrl = GameArtwork.GetCover(item.Game),
                BannerImageUrl = GameArtwork.GetBanner(item.Game)
            });
        }
        return gameDTOs;
    }

    public async Task<GameListItemDTO?> AddGameToWishlistAsync(Guid gameId)
    {
        var userId = _currentUser.UserId;
        if (await _games.GetByIdAsync(gameId, CancellationToken.None) == null)
            throw new KeyNotFoundException("Game was not found.");

        var existing = await _repository.GetAsync(userId, gameId);

        if (existing != null)
            return null;

        var wishlistEntry = new UserWishlist
        {
            UserId = userId,
            GameId = gameId
        };

        await _repository.AddAsync(wishlistEntry);

        var created = await _repository.GetAsync(userId, gameId);
        if (created?.Game == null)
            return null;

        return new GameListItemDTO
        {
            Id = created.Game.Id,
            Title = created.Game.Name,
            Price = created.Game.Price,
            ReleaseDate = created.Game.ReleaseDate,
            CoverImageUrl = GameArtwork.GetCover(created.Game),
            BannerImageUrl = GameArtwork.GetBanner(created.Game)
        };
    }
    public async Task RemoveGameFromWishlistAsync(Guid gameId)
    {
        var userId = _currentUser.UserId;
        var existing = await _repository.GetAsync(userId, gameId);
        if (existing == null)
            return;
        await _repository.RemoveAsync(existing);
    }
}
