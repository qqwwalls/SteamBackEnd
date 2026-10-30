using Steam.Application.Interfaces.Repository;
using Steam.Domain.Model;
using Microsoft.EntityFrameworkCore;
using Steam.Infrastructure.Data;
using System;
using System.Collections.Generic;
using System.Text;

namespace Steam.Infrastructure.Repositories;

public class UserRepository : IUserRepository
{
    private readonly SteamDbContext _context;

    public UserRepository(SteamDbContext context)
    {
        _context = context;
    }

    public async Task<User?> GetByIdAsync(Guid id, CancellationToken ct)
    {
        return await _context.Users.Include(u => u.Avatar).FirstOrDefaultAsync(u => u.Id == id, ct);
    }

    public async Task<User?> GetByEmailAsync(string email, CancellationToken ct)
    {
        var normalized = email.ToUpperInvariant();
        return await _context.Users.Include(u => u.Avatar).FirstOrDefaultAsync(u => u.Email.ToUpper() == normalized, ct);
    }

    public async Task<User?> GetByUsernameAsync(string username, CancellationToken ct)
    {
        var normalized = username.ToUpperInvariant();
        return await _context.Users.FirstOrDefaultAsync(u => u.Username.ToUpper() == normalized, ct);
    }

    public async Task AddAsync(User user, CancellationToken ct)
    {
        await _context.Users.AddAsync(user, ct);
        await _context.SaveChangesAsync(ct);
    }

    public async Task UpdateAsync(User user, CancellationToken ct)
    {
        _context.Users.Update(user);
        await _context.SaveChangesAsync(ct);
    }
}
