using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DAP.SqlNotebook.BL.DataAccess.Entities;
using DAP.SqlNotebook.Contract.Entities;
using Microsoft.EntityFrameworkCore;

namespace DAP.SqlNotebook.BL.DataAccess;

public sealed class NotebookFavoritesRepository : INotebookFavoritesRepository
{
    private readonly SqlNotebookDbContext _db;

    public NotebookFavoritesRepository(SqlNotebookDbContext db)
    {
        _db = db ?? throw new ArgumentNullException(nameof(db));
    }

    public async Task<IReadOnlyList<FavoriteFolderInfo>> GetFoldersAsync(string userLogin, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(userLogin))
            return Array.Empty<FavoriteFolderInfo>();
        var list = await _db.UserFavoriteFolders
            .AsNoTracking()
            .Where(x => x.UserLogin == userLogin)
            .OrderBy(x => x.Name)
            .Select(x => new FavoriteFolderInfo { Id = x.Id, Name = x.Name, ParentId = x.ParentId })
            .ToListAsync(ct)
            .ConfigureAwait(false);
        return list;
    }

    public async Task<Guid> CreateFolderAsync(string userLogin, string name, Guid? parentId, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(userLogin))
            throw new ArgumentException("User login required.", nameof(userLogin));
        var entity = new UserFavoriteFolderEntity
        {
            Id = Guid.NewGuid(),
            UserLogin = userLogin,
            Name = (name ?? string.Empty).Trim(),
            ParentId = parentId
        };
        if (string.IsNullOrWhiteSpace(entity.Name))
            throw new ArgumentException("Folder name required.", nameof(name));
        _db.UserFavoriteFolders.Add(entity);
        await _db.SaveChangesAsync(ct).ConfigureAwait(false);
        return entity.Id;
    }

    public async Task DeleteFolderAsync(string userLogin, Guid folderId, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(userLogin))
            return;
        var folder = await _db.UserFavoriteFolders
            .FirstOrDefaultAsync(x => x.UserLogin == userLogin && x.Id == folderId, ct)
            .ConfigureAwait(false);
        if (folder == null)
            return;
        _db.UserFavoriteFolders.Remove(folder);
        await _db.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<FavoriteNotebookItemInfo>> GetFavoriteNotebooksAsync(string userLogin, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(userLogin))
            return Array.Empty<FavoriteNotebookItemInfo>();
        var list = await (from f in _db.UserNotebookFavorites.AsNoTracking()
                         join n in _db.Notebooks.AsNoTracking() on f.NotebookId equals n.Id
                         where f.UserLogin == userLogin
                         select new FavoriteNotebookItemInfo
                         {
                             NotebookId = f.NotebookId,
                             Name = n.Name,
                             WorkspaceId = n.WorkspaceId,
                             FolderId = f.FolderId
                         })
            .ToListAsync(ct)
            .ConfigureAwait(false);
        return list;
    }

    public async Task AddNotebookToFavoritesAsync(string userLogin, Guid notebookId, Guid? folderId, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(userLogin))
            return;
        var exists = await _db.UserNotebookFavorites
            .AnyAsync(x => x.UserLogin == userLogin && x.NotebookId == notebookId, ct)
            .ConfigureAwait(false);
        if (exists)
        {
            await SetNotebookFolderAsync(userLogin, notebookId, folderId, ct).ConfigureAwait(false);
            return;
        }
        _db.UserNotebookFavorites.Add(new UserNotebookFavoriteEntity
        {
            UserLogin = userLogin,
            NotebookId = notebookId,
            FolderId = folderId
        });
        await _db.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    public async Task RemoveNotebookFromFavoritesAsync(string userLogin, Guid notebookId, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(userLogin))
            return;
        var entity = await _db.UserNotebookFavorites
            .FirstOrDefaultAsync(x => x.UserLogin == userLogin && x.NotebookId == notebookId, ct)
            .ConfigureAwait(false);
        if (entity != null)
        {
            _db.UserNotebookFavorites.Remove(entity);
            await _db.SaveChangesAsync(ct).ConfigureAwait(false);
        }
    }

    public async Task SetNotebookFolderAsync(string userLogin, Guid notebookId, Guid? folderId, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(userLogin))
            return;
        var entity = await _db.UserNotebookFavorites
            .FirstOrDefaultAsync(x => x.UserLogin == userLogin && x.NotebookId == notebookId, ct)
            .ConfigureAwait(false);
        if (entity != null)
        {
            entity.FolderId = folderId;
            await _db.SaveChangesAsync(ct).ConfigureAwait(false);
        }
    }

    public async Task<HashSet<Guid>> GetFavoriteNotebookIdsAsync(string userLogin, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(userLogin))
            return new HashSet<Guid>();
        var list = await _db.UserNotebookFavorites
            .AsNoTracking()
            .Where(x => x.UserLogin == userLogin)
            .Select(x => x.NotebookId)
            .ToListAsync(ct)
            .ConfigureAwait(false);
        return new HashSet<Guid>(list);
    }

    public async Task<bool> IsFavoriteAsync(string userLogin, Guid notebookId, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(userLogin))
            return false;
        return await _db.UserNotebookFavorites
            .AsNoTracking()
            .AnyAsync(x => x.UserLogin == userLogin && x.NotebookId == notebookId, ct)
            .ConfigureAwait(false);
    }
}
