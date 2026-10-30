using AutoMapper;
using Steam.Application.DTOs.SystemRequirementsDTOs;
using Steam.Application.Interfaces.Repository;
using Steam.Application.Interfaces.Services;
using Steam.Domain.Model;
using System;
using System.Collections.Generic;
using System.Text;

namespace Steam.Application.Services;

public class SystemRequirementsService : ISystemRequirementsService
{
    private readonly ISystemRequirementsRepository _repo;
    private readonly IMapper _mapper;
    private readonly IGameAccessService _access;

    public SystemRequirementsService(ISystemRequirementsRepository repo, IMapper mapper, IGameAccessService access)
    {
        _repo = repo;
        _mapper = mapper;
        _access = access;
    }

    public async Task<SystemRequirementsDTO> CreateAsync(SystemRequirementsCreateDTO dto, CancellationToken ct)
    {
        await _access.GetWritableAsync(dto.GameId, ct);
        var req = _mapper.Map<SystemRequirements>(dto);
        await _repo.AddAsync(req, ct);
        return _mapper.Map<SystemRequirementsDTO>(req);
    }

    public async Task<SystemRequirementsDTO?> GetByGameIdAsync(Guid gameId, CancellationToken ct)
    {
        var req = await _repo.GetByGameIdAsync(gameId, ct);
        return req == null ? null : _mapper.Map<SystemRequirementsDTO>(req);
    }
}
