using Steam.Application.Interfaces.Repository;
using Steam.Domain.Model;
using Steam.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;

namespace Steam.Infrastructure.Repositories;

public class WishlistRepository : IWishlistRepository
{
    private readonly SteamDbContext _context;

    public WishlistRepository(SteamDbContext context)
    {
        _context = context;
    }
    public async Task<List<UserWishlist>> GetByUserIdAsync(Guid userId)
    {
        return await _context.UserWishlists
            .Where(w => w.UserId == userId)
            .Include(w => w.Game)
            .ThenInclude(g => g.Media)
            .ToListAsync();
    }
    public async Task<UserWishlist?> GetAsync(Guid userId, Guid gameId)
    {
        return await _context.UserWishlists
            .Include(w => w.Game)
            .ThenInclude(g => g.Media)
            .FirstOrDefaultAsync(w => w.UserId == userId && w.GameId == gameId);
    }
    public async Task AddAsync(UserWishlist wishlist)
    {
        await _context.UserWishlists.AddAsync(wishlist);
        await _context.SaveChangesAsync();
    }

    public async Task RemoveAsync(UserWishlist wishlist)
    {
        _context.UserWishlists.Remove(wishlist);
        await _context.SaveChangesAsync();
    }
}
