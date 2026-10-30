using Microsoft.AspNetCore.Mvc;
using Steam.Application.DTOs.UserDTOs;
using Microsoft.AspNetCore.Authorization;
using Steam.Application.Interfaces.Services;

namespace Steam.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UserController : ControllerBase
{
    private readonly IUserService _service;

    public UserController(IUserService service)
    {
        _service = service;
    }

    [HttpGet("{id:guid}")]
    [Authorize]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var user = await _service.GetByIdAsync(id, ct);
        return user == null ? NotFound() : Ok(user);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] UserCreateDTO dto, CancellationToken ct)
    {
        var user = await _service.CreateAsync(dto, ct);
        return Created($"api/user/{user.Id}", user);
    }

    [HttpPut("{id:guid}/avatar")]
    [Authorize]
    public async Task<IActionResult> UpdateAvatar(Guid id, [FromBody] UpdateAvatarDTO dto, CancellationToken ct)
    {
        var user = await _service.UpdateAvatarAsync(id, dto.AvatarUrl, ct);
        return user == null ? NotFound() : Ok(user);
    }
}
