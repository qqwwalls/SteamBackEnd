using Steam.Application.DTOs;
using Steam.Application.DTOs.AuthDTOs;
using Steam.Application.DTOs.UserDTOs;
using System;
using System.Collections.Generic;
using System.Text;

namespace Steam.Application.Interfaces.Services;

public interface IAuthService
{
    Task<TokenResponseDTO> RegisterAsync(UserCreateDTO dto, CancellationToken ct);
    Task<TokenResponseDTO?> LoginAsync(UserLoginDto dto, CancellationToken ct);
    Task RevokeAsync(string refreshToken, CancellationToken ct);
    Task<TokenResponseDTO> LoginExternalAsync(ExternalAuthDTO dto, CancellationToken ct);
    Task<TokenResponseDTO?> RefreshAsync(string refreshToken, CancellationToken ct);
}
