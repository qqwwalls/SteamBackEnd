using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Steam.Application.DTOs.SystemRequirementsDTOs;
using Steam.Application.Interfaces.Repository;
using Steam.Application.Interfaces.Services;

namespace Steam.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SystemRequirementsController : ControllerBase
{
    private readonly ISystemRequirementsService _service;

    public SystemRequirementsController(ISystemRequirementsService service)
    {
        _service = service;
    }

    [HttpPost]
    [Authorize]
    public async Task<IActionResult> Create([FromBody] SystemRequirementsCreateDTO dto, CancellationToken ct)
    {
        var req = await _service.CreateAsync(dto, ct);
        return Created($"api/systemrequirements/{req.Id}", req);
    }

    [HttpGet("game/{gameId:guid}")]
    public async Task<IActionResult> GetByGameId(Guid gameId, CancellationToken ct)
    {
        var req = await _service.GetByGameIdAsync(gameId, ct);
        return req == null ? NotFound() : Ok(req);
    }
}
