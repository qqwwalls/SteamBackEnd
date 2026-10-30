using Steam.Domain.Model;
using System;
using System.Collections.Generic;
using System.Text;

namespace Steam.Application.Interfaces.Repository;

public interface IGameRepository
{
    Task<IEnumerable<Game>> GetAllAsync(CancellationToken ct);
    Task<Game?> GetByIdAsync(Guid id, CancellationToken ct);
    Task<List<Game>> GetByIdsAsync(IEnumerable<Guid> ids, CancellationToken ct);
    Task AddAsync(Game game, CancellationToken ct);
    Task UpdateAsync(Game game, CancellationToken ct);
}
