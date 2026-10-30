using AutoMapper;
using Steam.Application.DTOs.GameDTOs;
using Steam.Application.Interfaces.Repository;
using Steam.Application.Interfaces.Services;
using Steam.Domain.Model;
using System;
using System.Collections.Generic;
using System.Text;

namespace Steam.Application.Services;

public class GameService : IGameService
{
    private readonly IGameRepository _repo;
    private readonly IGenreRepository _genreRepo;
    private readonly ITagRepository _tagRepo;
    private readonly IMapper _mapper;
    private readonly IUserRepository _users;
    private readonly ICurrentUserService _currentUser;
    private readonly IGameAccessService _access;

    public GameService(
        IGameRepository repo,
        IGenreRepository genreRepo,
        ITagRepository tagRepo,
        IMapper mapper,
        IUserRepository users,
        ICurrentUserService currentUser,
        IGameAccessService access)
    {
        _repo = repo;
        _genreRepo = genreRepo;
        _tagRepo = tagRepo;
        _mapper = mapper;
        _users = users;
        _currentUser = currentUser;
        _access = access;
    }

    public async Task<GameDTO> CreateAsync(GameCreateDTO dto, CancellationToken ct)
    {
        if (!_currentUser.IsAdmin && dto.DeveloperId != _currentUser.UserId)
            throw new UnauthorizedAccessException();
        if (await _users.GetByIdAsync(dto.DeveloperId, ct) == null)
            throw new KeyNotFoundException("Developer was not found.");

        var game = new Game
        {
            Name = dto.Title,
            Description = dto.Description,
            Price = dto.Price,
            ReleaseDate = DateTime.UtcNow,
            DeveloperId = dto.DeveloperId
        };

        if (dto.GenreIds != null && dto.GenreIds.Any())
        {
            var genres = await _genreRepo.GetAllAsync(ct);
            var selectedGenres = genres.Where(g => dto.GenreIds.Contains(g.Id)).ToList();
            if (selectedGenres.Count != dto.GenreIds.Distinct().Count())
                throw new ArgumentException("One or more genres do not exist.");

            foreach (var genre in selectedGenres)
            {
                game.GameGenres.Add(new GameGenre
                {
                    GenreId = genre.Id
                });
            }
        }

        if (dto.TagIds != null && dto.TagIds.Any())
        {
            var tags = await _tagRepo.GetAllAsync(ct);
            var selectedTags = tags.Where(t => dto.TagIds.Contains(t.Id)).ToList();
            if (selectedTags.Count != dto.TagIds.Distinct().Count())
                throw new ArgumentException("One or more tags do not exist.");

            foreach (var tag in selectedTags)
            {
                game.GameTags.Add(new GameTag
                {
                    TagId = tag.Id
                });
            }
        }

        await _repo.AddAsync(game, ct);

        return _mapper.Map<GameDTO>(game);
    }

    public async Task<GameDTO?> UpdateAsync(Guid id, GameUpdateDTO dto, CancellationToken ct)
    {
        var game = await _repo.GetByIdAsync(id, ct);
        if (game == null)
            return null;

        await _access.GetWritableAsync(id, ct);
        _mapper.Map(dto, game);
        await _repo.UpdateAsync(game, ct);

        return _mapper.Map<GameDTO>(game);
    }

    public async Task<GameDTO?> GetByIdAsync(Guid id, CancellationToken ct)
    {
        var game = await _repo.GetByIdAsync(id, ct);
        return game == null ? null : _mapper.Map<GameDTO>(game);
    }

    public async Task<IEnumerable<GameListItemDTO>> GetAllAsync(CancellationToken ct)
    {
        var games = await _repo.GetAllAsync(ct);
        return _mapper.Map<IEnumerable<GameListItemDTO>>(games);
    }
}
