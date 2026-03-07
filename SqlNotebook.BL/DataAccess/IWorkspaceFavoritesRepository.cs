using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace DAP.SqlNotebook.BL.DataAccess;

public interface IWorkspaceFavoritesRepository
{
    Task<IReadOnlyList<Guid>> GetByUserAsync(string userLogin, CancellationToken ct = default);
    Task AddAsync(string userLogin, Guid workspaceId, CancellationToken ct = default);
    Task RemoveAsync(string userLogin, Guid workspaceId, CancellationToken ct = default);
    Task<bool> ExistsAsync(string userLogin, Guid workspaceId, CancellationToken ct = default);
}
