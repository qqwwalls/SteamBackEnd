using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Steam.Application.DTOs.AchievementDTOs;
using Steam.Application.Interfaces.Repository;
using Steam.Application.Interfaces.Services;

namespace Steam.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AchievementController : ControllerBase
{
    private readonly IAchievementService _service;

    public AchievementController(IAchievementService service)
    {
        _service = service;
    }

    [HttpPost]
    [Authorize]
    public async Task<IActionResult> Create([FromBody] AchievementCreateDTO dto, CancellationToken ct)
    {
        var achievement = await _service.CreateAsync(dto, ct);
        return Created($"api/achievement/{achievement.Id}", achievement);
    }

    [HttpGet("game/{gameId:guid}")]
    public async Task<IActionResult> GetByGameId(Guid gameId, CancellationToken ct)
    {
        return Ok(await _service.GetByGameIdAsync(gameId, ct));
    }
}
