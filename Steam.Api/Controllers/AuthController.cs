using Microsoft.AspNetCore.Mvc;
using Steam.Application.DTOs;
using Steam.Application.DTOs.AuthDTOs;
using Steam.Application.DTOs.UserDTOs;
using Steam.Application.Interfaces;
using Steam.Application.Interfaces.Services;

namespace Steam.Api.Controllers;


[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _auth;

    public AuthController(IAuthService auth)
    {
        _auth = auth;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] UserCreateDTO dto, CancellationToken ct)
    {
        return Ok(await _auth.RegisterAsync(dto, ct));
    }

    [HttpPost("login")]
    [HttpPost("~/api/user/auth/login")]
    public async Task<IActionResult> Login([FromBody] UserLoginDto dto, CancellationToken ct)
    {
        var result = await _auth.LoginAsync(dto, ct);
        return result == null ? Unauthorized(new { message = "Invalid email or password." }) : Ok(result);
    }

    [HttpPost("logout")]
    public async Task<IActionResult> Logout([FromBody] RefreshTokenRequestDTO dto, CancellationToken ct)
    {
        await _auth.RevokeAsync(dto.RefreshToken, ct);
        return NoContent();
    }

    [HttpPost("external-login")]
    public IActionResult ExternalLogin([FromBody] ExternalAuthDTO dto, CancellationToken ct)
    {
        // A client-supplied email is not proof of an external identity.
        return StatusCode(501, new { message = "External login requires a configured identity provider and verified provider tokens." });
    }

    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh([FromBody] RefreshTokenRequestDTO dto, CancellationToken ct)
    {
        var result = await _auth.RefreshAsync(dto.RefreshToken, ct);
        return result == null ? Unauthorized() : Ok(result);
    }
}
