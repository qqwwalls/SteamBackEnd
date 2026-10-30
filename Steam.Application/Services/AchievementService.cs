using AutoMapper;
using Steam.Application.DTOs.AchievementDTOs;
using Steam.Application.Interfaces.Repository;
using Steam.Application.Interfaces.Services;
using Steam.Domain.Model;
using System;
using System.Collections.Generic;
using System.Text;

namespace Steam.Application.Services;

public class AchievementService : IAchievementService
{
    private readonly IAchievementRepository _repo;
    private readonly IMapper _mapper;
    private readonly IGameAccessService _access;
    private readonly IMediaRepository _media;

    public AchievementService(IAchievementRepository repo, IMapper mapper, IGameAccessService access, IMediaRepository media)
    {
        _repo = repo;
        _mapper = mapper;
        _access = access;
        _media = media;
    }

    public async Task<AchievementDTO> CreateAsync(AchievementCreateDTO dto, CancellationToken ct)
    {
        await _access.GetWritableAsync(dto.GameId, ct);
        if (dto.IconId.HasValue && await _media.GetByIdAsync(dto.IconId.Value, ct) == null)
            throw new KeyNotFoundException("Achievement icon was not found.");
        var achievement = _mapper.Map<Achievement>(dto);
        await _repo.AddAsync(achievement, ct);
        return _mapper.Map<AchievementDTO>(achievement);
    }

    public async Task<IEnumerable<AchievementDTO>> GetByGameIdAsync(Guid gameId, CancellationToken ct)
    {
        var achievements = await _repo.GetByGameIdAsync(gameId, ct);
        return _mapper.Map<IEnumerable<AchievementDTO>>(achievements);
    }
}
