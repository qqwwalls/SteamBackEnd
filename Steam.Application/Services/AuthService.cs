using Steam.Application.DTOs;
using Steam.Application.DTOs.AuthDTOs;
using Steam.Application.DTOs.UserDTOs;
using System.IdentityModel.Tokens.Jwt;
using Steam.Application.Interfaces.Repository;
using Steam.Application.Interfaces.Services;
using Steam.Domain.Enum;
using Steam.Domain.Model;
using System;
using System.Collections.Generic;
using System.Text;

namespace Steam.Application.Services;

public class AuthService : IAuthService
{
    private readonly IAuthRepository _repo;
    private readonly IJWTService _jwt;
    private readonly IUserRepository _users;
    private readonly IUserService _userService;

    public AuthService(IAuthRepository repo, IJWTService jwt, IUserRepository users, IUserService userService)
    {
        _repo = repo;
        _jwt = jwt;
        _users = users;
        _userService = userService;
    }

    public async Task<TokenResponseDTO> RegisterAsync(UserCreateDTO dto, CancellationToken ct)
    {
        var created = await _userService.CreateAsync(dto, ct);
        var user = await _repo.GetByIdAsync(created.Id, ct)
            ?? throw new KeyNotFoundException("User was not found.");
        return await IssueTokensAsync(user, ct);
    }

    public async Task<TokenResponseDTO?> LoginAsync(UserLoginDto dto, CancellationToken ct)
    {
        var user = await _users.GetByEmailAsync(dto.Email.Trim(), ct);
        if (user == null || !user.IsActive || !user.PasswordHash.StartsWith("$2") ||
            !BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash))
            return null;
        return await IssueTokensAsync(user, ct);
    }

    public Task RevokeAsync(string refreshToken, CancellationToken ct)
        => _repo.RevokeRefreshTokenAsync(refreshToken, ct);

    private async Task<TokenResponseDTO> IssueTokensAsync(User user, CancellationToken ct)
    {
        var access = _jwt.GenerateAccessToken(user);
        var refresh = _jwt.CreateRefreshToken(user.Id);
        await _repo.SaveRefreshTokenAsync(refresh, ct);
        return CreateResponse(user, access, refresh);
    }

    private static TokenResponseDTO CreateResponse(User user, string access, RefreshToken refresh)
    {
        return new TokenResponseDTO
        {
            AccessToken = access,
            RefreshToken = refresh.Token,
            ExpiresAt = new JwtSecurityTokenHandler().ReadJwtToken(access).ValidTo,
            RefreshTokenExpiresAt = refresh.ExpiresAt,
            User = new UserDTO { Id = user.Id, Username = user.Username, Email = user.Email, AvatarUrl = user.Avatar?.Url }
        };
    }

    public async Task<TokenResponseDTO> LoginExternalAsync(ExternalAuthDTO dto, CancellationToken ct)
    {
        var user = await _repo.GetByEmailAsync(dto.Email, ct);

        if (user == null)
        {
            user = new User
            {
                Id = Guid.NewGuid(),
                Email = dto.Email,
                Username = dto.Name,
                Role = UserRole.User
            };

            await _repo.AddUserAsync(user, ct);
        }

        var access = _jwt.GenerateAccessToken(user);
        var refresh = _jwt.CreateRefreshToken(user.Id);

        await _repo.SaveRefreshTokenAsync(refresh, ct);

        return new TokenResponseDTO
        {
            AccessToken = access,
            RefreshToken = refresh.Token,
            ExpiresAt = refresh.ExpiresAt
        };
    }

    public async Task<TokenResponseDTO?> RefreshAsync(string refreshToken, CancellationToken ct)
    {
        var token = await _repo.GetRefreshTokenAsync(refreshToken, ct);
        if (token == null || token.IsRevoked || token.ExpiresAt <= DateTime.UtcNow)
            return null;

        var user = await _repo.GetByIdAsync(token.UserId, ct);
        if (user == null || !user.IsActive)
            return null;

        var access = _jwt.GenerateAccessToken(user);
        var newRefresh = _jwt.CreateRefreshToken(user.Id);

        if (!await _repo.RotateRefreshTokenAsync(refreshToken, newRefresh, ct))
            return null;

        return CreateResponse(user, access, newRefresh);
    }
}
