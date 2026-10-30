using AutoMapper;
using Steam.Application.DTOs.UserDTOs;
using Steam.Application.Exceptions;
using Steam.Application.Interfaces.Repository;
using Steam.Application.Interfaces.Services;
using Steam.Domain.Enum;
using Steam.Domain.Model;
using System;
using System.Collections.Generic;
using System.Text;

namespace Steam.Application.Services;

public class UserService : IUserService
{
    private readonly IUserRepository _repo;
    private readonly IMediaRepository _mediaRepo;
    private readonly IMapper _mapper;
    private readonly ICurrentUserService _currentUser;

    public UserService(IUserRepository repo, IMediaRepository mediaRepo, IMapper mapper, ICurrentUserService currentUser)
    {
        _repo = repo;
        _mediaRepo = mediaRepo;
        _mapper = mapper;
        _currentUser = currentUser;
    }

    public async Task<UserDTO?> GetByIdAsync(Guid id, CancellationToken ct)
    {
        if (!_currentUser.IsAdmin && id != _currentUser.UserId)
            throw new UnauthorizedAccessException();
        var user = await _repo.GetByIdAsync(id, ct);
        return user == null ? null : _mapper.Map<UserDTO>(user);
    }

    public async Task<UserDTO> CreateAsync(UserCreateDTO dto, CancellationToken ct)
    {
        dto.Email = dto.Email.Trim().ToLowerInvariant();
        dto.Username = dto.Username.Trim();
        if (await _repo.GetByEmailAsync(dto.Email, ct) != null ||
            await _repo.GetByUsernameAsync(dto.Username, ct) != null)
            throw new ConflictException("A user with this email or username already exists.");

        var user = new User
        {
            Id = Guid.NewGuid(),
            Username = dto.Username,
            Email = dto.Email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
            Description = "",
            Role = UserRole.User,
            IsActive = true,
            IsVerified = false
        };

        if (!string.IsNullOrWhiteSpace(dto.AvatarUrl))
        {
            var media = new Media
            {
                Url = dto.AvatarUrl,
                Type = MediaType.Image
            };

            await _mediaRepo.AddAsync(media, ct);
            user.AvatarId = media.Id;
        }

        await _repo.AddAsync(user, ct);

        return _mapper.Map<UserDTO>(user);
    }

    public async Task<UserDTO?> UpdateAvatarAsync(Guid id, string avatarUrl, CancellationToken ct)
    {
        if (!_currentUser.IsAdmin && id != _currentUser.UserId)
            throw new UnauthorizedAccessException();
        var user = await _repo.GetByIdAsync(id, ct);
        if (user == null)
            return null;

        var media = new Media
        {
            Url = avatarUrl,
            Type = MediaType.Image
        };

        await _mediaRepo.AddAsync(media, ct);

        user.AvatarId = media.Id;
        user.Avatar = media;

        await _repo.UpdateAsync(user, ct);

        return _mapper.Map<UserDTO>(user);
    }
}
