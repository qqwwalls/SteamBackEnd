using Steam.Application.Interfaces.Repository;
using Steam.Domain.Model;
using Microsoft.EntityFrameworkCore;
using Steam.Infrastructure.Data;
using System;
using System.Collections.Generic;
using System.Text;

namespace Steam.Infrastructure.Repositories;

public class GameRepository : IGameRepository
{
    private readonly SteamDbContext _context;

    public GameRepository(SteamDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<Game>> GetAllAsync(CancellationToken ct)
    {
        return await _context.Games
            .Include(g => g.GameGenres)
            .Include(g => g.GameTags)
            .Include(g => g.Media)
            .OrderBy(g => g.Name)
            .ToListAsync(ct);
    }

    public async Task<Game?> GetByIdAsync(Guid id, CancellationToken ct)
    {
        return await _context.Games
            .Include(g => g.GameGenres)
            .Include(g => g.GameTags)
            .Include(g => g.Media)
            .Include(g => g.Developer)
            .Include(g => g.SystemRequirements)
            .Include(g => g.Achievements)
            .FirstOrDefaultAsync(g => g.Id == id, ct);
    }

    public async Task<List<Game>> GetByIdsAsync(IEnumerable<Guid> ids, CancellationToken ct)
    {
        return await _context.Games.Include(g => g.Media).Where(g => ids.Contains(g.Id)).ToListAsync(ct);
    }

    public async Task AddAsync(Game game, CancellationToken ct)
    {
        await _context.Games.AddAsync(game, ct);
        await _context.SaveChangesAsync(ct);
    }

    public async Task UpdateAsync(Game game, CancellationToken ct)
    {
        _context.Games.Update(game);
        await _context.SaveChangesAsync(ct);
    }
}
