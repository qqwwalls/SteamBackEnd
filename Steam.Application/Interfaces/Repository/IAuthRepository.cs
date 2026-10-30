using Steam.Domain.Model;
using System;
using System.Collections.Generic;
using System.Text;

namespace Steam.Application.Interfaces.Repository;

public interface IAuthRepository
{
    Task<User?> GetByEmailAsync(string email, CancellationToken ct);
    Task<User?> GetByIdAsync(Guid id, CancellationToken ct);
    Task AddUserAsync(User user, CancellationToken ct);
    Task SaveRefreshTokenAsync(RefreshToken token, CancellationToken ct);
    Task<RefreshToken?> GetRefreshTokenAsync(string token, CancellationToken ct);
    Task<bool> RotateRefreshTokenAsync(string token, RefreshToken replacement, CancellationToken ct);
    Task RevokeRefreshTokenAsync(string token, CancellationToken ct);
}
