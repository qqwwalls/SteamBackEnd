using Steam.Domain.Model;

namespace Steam.Application.Interfaces.Repository;

public interface ILibraryRepository
{
    Task<List<UserGame>> GetByUserIdAsync(Guid userId, CancellationToken ct);
    Task AddAsync(IEnumerable<UserGame> games, CancellationToken ct);
}
