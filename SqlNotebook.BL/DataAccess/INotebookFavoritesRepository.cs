using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using DAP.SqlNotebook.Contract.Entities;

namespace DAP.SqlNotebook.BL.DataAccess;

public interface INotebookFavoritesRepository
{
    Task<IReadOnlyList<FavoriteFolderInfo>> GetFoldersAsync(string userLogin, CancellationToken ct = default);
    Task<Guid> CreateFolderAsync(string userLogin, string name, Guid? parentId, CancellationToken ct = default);
    Task DeleteFolderAsync(string userLogin, Guid folderId, CancellationToken ct = default);
    Task<IReadOnlyList<FavoriteNotebookItemInfo>> GetFavoriteNotebooksAsync(string userLogin, CancellationToken ct = default);
    Task AddNotebookToFavoritesAsync(string userLogin, Guid notebookId, Guid? folderId, CancellationToken ct = default);
    Task RemoveNotebookFromFavoritesAsync(string userLogin, Guid notebookId, CancellationToken ct = default);
    Task SetNotebookFolderAsync(string userLogin, Guid notebookId, Guid? folderId, CancellationToken ct = default);
    Task<HashSet<Guid>> GetFavoriteNotebookIdsAsync(string userLogin, CancellationToken ct = default);
    Task<bool> IsFavoriteAsync(string userLogin, Guid notebookId, CancellationToken ct = default);
}
