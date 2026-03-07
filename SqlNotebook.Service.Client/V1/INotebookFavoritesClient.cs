using DAP.SqlNotebook.Contract.Entities;

namespace DAP.SqlNotebook.Service.Client.V1;

public interface INotebookFavoritesClient
{
    Task<IReadOnlyList<FavoriteFolderInfo>> GetFoldersAsync(CancellationToken ct);
    Task<FavoriteFolderInfo> CreateFolderAsync(string name, Guid? parentId, CancellationToken ct);
    Task DeleteFolderAsync(Guid folderId, CancellationToken ct);
    Task<IReadOnlyList<FavoriteNotebookItemInfo>> GetFavoriteNotebooksAsync(CancellationToken ct);
    Task AddNotebookToFavoritesAsync(Guid notebookId, Guid? folderId, CancellationToken ct);
    Task RemoveNotebookFromFavoritesAsync(Guid notebookId, CancellationToken ct);
    Task SetNotebookFolderAsync(Guid notebookId, Guid? folderId, CancellationToken ct);
    Task<HashSet<Guid>> GetFavoriteNotebookIdsAsync(CancellationToken ct);
}
