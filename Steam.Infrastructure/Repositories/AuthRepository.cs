using Steam.Domain.Model;
using Steam.Infrastructure.Data;
using Steam.Application.Interfaces.Repository;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;

namespace Steam.Infrastructure.Repositories;

public class AuthRepository : IAuthRepository
{
    private readonly SteamDbContext _context;

    public AuthRepository(SteamDbContext context)
    {
        _context = context;
    }

    public async Task<User?> GetByEmailAsync(string email, CancellationToken ct)
    {
        return await _context.Users.FirstOrDefaultAsync(u => u.Email == email, ct);
    }

    public async Task<User?> GetByIdAsync(Guid id, CancellationToken ct)
    {
        return await _context.Users.Include(u => u.Avatar).FirstOrDefaultAsync(u => u.Id == id, ct);
    }

    public async Task AddUserAsync(User user, CancellationToken ct)
    {
        await _context.Users.AddAsync(user, ct);
        await _context.SaveChangesAsync(ct);
    }

    public async Task SaveRefreshTokenAsync(RefreshToken token, CancellationToken ct)
    {
        await _context.RefreshTokens.AddAsync(token, ct);
        await _context.SaveChangesAsync(ct);
    }

    public async Task<RefreshToken?> GetRefreshTokenAsync(string token, CancellationToken ct)
    {
        return await _context.RefreshTokens.FirstOrDefaultAsync(t => t.Token == token, ct);
    }

    public async Task<bool> RotateRefreshTokenAsync(string token, RefreshToken replacement, CancellationToken ct)
    {
        await using var transaction = await _context.Database.BeginTransactionAsync(ct);
        var updated = await _context.RefreshTokens
            .Where(t => t.Token == token && !t.IsRevoked && t.ExpiresAt > DateTime.UtcNow)
            .ExecuteUpdateAsync(setters => setters.SetProperty(t => t.IsRevoked, true), ct);
        if (updated == 0)
            return false;

        await _context.RefreshTokens.AddAsync(replacement, ct);
        await _context.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
        return true;
    }

    public async Task RevokeRefreshTokenAsync(string token, CancellationToken ct)
    {
        await _context.RefreshTokens.Where(t => t.Token == token)
            .ExecuteUpdateAsync(setters => setters.SetProperty(t => t.IsRevoked, true), ct);
    }
}
