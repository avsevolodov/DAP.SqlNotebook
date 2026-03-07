using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DAP.SqlNotebook.BL.DataAccess.Entities;
using Microsoft.EntityFrameworkCore;

namespace DAP.SqlNotebook.BL.DataAccess;

public sealed class WorkspaceFavoritesRepository : IWorkspaceFavoritesRepository
{
    private readonly SqlNotebookDbContext _db;

    public WorkspaceFavoritesRepository(SqlNotebookDbContext db)
    {
        _db = db ?? throw new ArgumentNullException(nameof(db));
    }

    public async Task<IReadOnlyList<Guid>> GetByUserAsync(string userLogin, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(userLogin))
            return Array.Empty<Guid>();
        var list = await _db.UserWorkspaceFavorites
            .AsNoTracking()
            .Where(x => x.UserLogin == userLogin)
            .Select(x => x.WorkspaceId)
            .ToListAsync(ct)
            .ConfigureAwait(false);
        return list;
    }

    public async Task AddAsync(string userLogin, Guid workspaceId, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(userLogin))
            return;
        var exists = await _db.UserWorkspaceFavorites
            .AnyAsync(x => x.UserLogin == userLogin && x.WorkspaceId == workspaceId, ct)
            .ConfigureAwait(false);
        if (exists)
            return;
        _db.UserWorkspaceFavorites.Add(new UserWorkspaceFavoriteEntity
        {
            UserLogin = userLogin,
            WorkspaceId = workspaceId
        });
        await _db.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    public async Task RemoveAsync(string userLogin, Guid workspaceId, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(userLogin))
            return;
        var entity = await _db.UserWorkspaceFavorites
            .FirstOrDefaultAsync(x => x.UserLogin == userLogin && x.WorkspaceId == workspaceId, ct)
            .ConfigureAwait(false);
        if (entity != null)
        {
            _db.UserWorkspaceFavorites.Remove(entity);
            await _db.SaveChangesAsync(ct).ConfigureAwait(false);
        }
    }

    public async Task<bool> ExistsAsync(string userLogin, Guid workspaceId, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(userLogin))
            return false;
        return await _db.UserWorkspaceFavorites
            .AsNoTracking()
            .AnyAsync(x => x.UserLogin == userLogin && x.WorkspaceId == workspaceId, ct)
            .ConfigureAwait(false);
    }
}
