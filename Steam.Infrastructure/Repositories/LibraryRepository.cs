using Microsoft.EntityFrameworkCore;
using Steam.Application.Interfaces.Repository;
using Steam.Domain.Model;
using Steam.Infrastructure.Data;

namespace Steam.Infrastructure.Repositories;

public class LibraryRepository : ILibraryRepository
{
    private readonly SteamDbContext _context;

    public LibraryRepository(SteamDbContext context) => _context = context;

    public Task<List<UserGame>> GetByUserIdAsync(Guid userId, CancellationToken ct)
        => _context.UserGames.AsNoTracking().Where(item => item.UserId == userId)
            .Include(item => item.Game).ThenInclude(game => game.Media)
            .OrderByDescending(item => item.PurchasedAt).ToListAsync(ct);

    public async Task AddAsync(IEnumerable<UserGame> games, CancellationToken ct)
    {
        await _context.UserGames.AddRangeAsync(games, ct);
        await _context.SaveChangesAsync(ct);
    }
}
