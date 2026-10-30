using System;
using System.Collections.Generic;
using System.Text;

namespace Steam.Application.DTOs.AuthDTOs;

public class TokenResponseDTO
{
    public string AccessToken { get; set; } = string.Empty;
    public string RefreshToken { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public DateTime RefreshTokenExpiresAt { get; set; }
    public Steam.Application.DTOs.UserDTOs.UserDTO? User { get; set; }
}
