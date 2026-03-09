using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DAP.SqlNotebook.BL.DataAccess.Entities;
using Microsoft.EntityFrameworkCore;

namespace DAP.SqlNotebook.BL.DataAccess;

public sealed class WorkspaceRepository : IWorkspaceRepository
{
    private readonly SqlNotebookDbContext _db;

    public WorkspaceRepository(SqlNotebookDbContext db)
    {
        _db = db ?? throw new ArgumentNullException(nameof(db));
    }

    public async Task<WorkspaceEntity?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        return await _db.Workspaces.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, ct).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<WorkspaceEntity>> GetAllAsync(CancellationToken ct = default)
    {
        return await _db.Workspaces.AsNoTracking().OrderBy(x => x.Name).ToListAsync(ct).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<WorkspaceEntity>> GetByOwnerAsync(string? ownerLogin, CancellationToken ct = default)
    {
        IQueryable<WorkspaceEntity> query = _db.Workspaces.AsNoTracking();
        if (!string.IsNullOrEmpty(ownerLogin))
            query = query.Where(x => x.OwnerLogin == ownerLogin);
        return await query.OrderBy(x => x.Name).ToListAsync(ct).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<WorkspaceEntity>> GetTreeAsync(CancellationToken ct = default)
    {
        return await _db.Workspaces
            .AsNoTracking()
            .OrderBy(x => x.ParentId == null ? 0 : 1)
            .ThenBy(x => x.Name)
            .ToListAsync(ct)
            .ConfigureAwait(false);
    }

    public async Task<WorkspaceEntity> CreateAsync(WorkspaceEntity entity, CancellationToken ct = default)
    {
        if (entity.Id == default)
            entity.Id = Guid.NewGuid();
        // OwnerLogin set by caller (current user)
        _db.Workspaces.Add(entity);
        await _db.SaveChangesAsync(ct).ConfigureAwait(false);
        return entity;
    }

    public async Task UpdateAsync(WorkspaceEntity entity, CancellationToken ct = default)
    {
        var existing = await _db.Workspaces.FirstOrDefaultAsync(x => x.Id == entity.Id, ct).ConfigureAwait(false);
        if (existing == null)
            throw new InvalidOperationException($"Workspace {entity.Id} not found.");
        existing.Name = entity.Name;
        existing.Description = entity.Description;
        existing.ParentId = entity.ParentId;
        existing.IsFolder = entity.IsFolder;
        existing.Icon = entity.Icon;
        existing.Visibility = entity.Visibility;
        if (entity.OwnerLogin != null)
            existing.OwnerLogin = entity.OwnerLogin;
        await _db.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var entity = await _db.Workspaces.FirstOrDefaultAsync(x => x.Id == id, ct).ConfigureAwait(false);
        if (entity != null)
        {
            _db.Workspaces.Remove(entity);
            await _db.SaveChangesAsync(ct).ConfigureAwait(false);
        }
    }
}
