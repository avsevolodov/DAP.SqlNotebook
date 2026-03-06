using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using DAP.SqlNotebook.BL.DataAccess.Entities;

namespace DAP.SqlNotebook.BL.DataAccess;

public interface IWorkspaceRepository
{
    Task<WorkspaceEntity?> GetByIdAsync(Guid id, CancellationToken ct = default);
    /// <summary>All workspaces (no filter).</summary>
    Task<IReadOnlyList<WorkspaceEntity>> GetAllAsync(CancellationToken ct = default);
    /// <summary>Workspaces owned by the given login; if login is null, returns all.</summary>
    Task<IReadOnlyList<WorkspaceEntity>> GetByOwnerAsync(string? ownerLogin, CancellationToken ct = default);
    Task<WorkspaceEntity> CreateAsync(WorkspaceEntity entity, CancellationToken ct = default);
    Task UpdateAsync(WorkspaceEntity entity, CancellationToken ct = default);
    Task DeleteAsync(Guid id, CancellationToken ct = default);
}
