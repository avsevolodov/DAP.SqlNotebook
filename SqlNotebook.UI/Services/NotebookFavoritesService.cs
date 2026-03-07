using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using DAP.SqlNotebook.Contract.Entities;
using DAP.SqlNotebook.Service.Client.V1;

namespace DAP.SqlNotebook.UI.Services;

/// <summary>Notebook favorites and folders in Favorites section (server-side).</summary>
public interface INotebookFavoritesService
{
    Task<HashSet<Guid>> GetFavoriteNotebookIdsAsync();
    Task<IReadOnlyList<FavoriteFolderInfo>> GetFoldersAsync();
    Task<IReadOnlyList<FavoriteNotebookItemInfo>> GetFavoriteNotebooksAsync();
    Task<FavoriteFolderInfo> CreateFolderAsync(string name, Guid? parentId = null);
    Task DeleteFolderAsync(Guid folderId);
    Task ToggleNotebookFavoriteAsync(Guid notebookId, Guid? folderId = null);
    Task SetNotebookFolderAsync(Guid notebookId, Guid? folderId);
    Task RemoveNotebookFromFavoritesAsync(Guid notebookId);
}

public sealed class NotebookFavoritesService : INotebookFavoritesService
{
    private readonly INotebookFavoritesClient _client;

    public NotebookFavoritesService(INotebookFavoritesClient client)
    {
        _client = client ?? throw new ArgumentNullException(nameof(client));
    }

    public Task<HashSet<Guid>> GetFavoriteNotebookIdsAsync()
        => _client.GetFavoriteNotebookIdsAsync(CancellationToken.None);

    public Task<IReadOnlyList<FavoriteFolderInfo>> GetFoldersAsync()
        => _client.GetFoldersAsync(CancellationToken.None);

    public Task<IReadOnlyList<FavoriteNotebookItemInfo>> GetFavoriteNotebooksAsync()
        => _client.GetFavoriteNotebooksAsync(CancellationToken.None);

    public Task<FavoriteFolderInfo> CreateFolderAsync(string name, Guid? parentId = null)
        => _client.CreateFolderAsync(name, parentId, CancellationToken.None);

    public Task DeleteFolderAsync(Guid folderId)
        => _client.DeleteFolderAsync(folderId, CancellationToken.None);

    public async Task ToggleNotebookFavoriteAsync(Guid notebookId, Guid? folderId = null)
    {
        var ids = await _client.GetFavoriteNotebookIdsAsync(CancellationToken.None).ConfigureAwait(false);
        if (ids.Contains(notebookId))
            await _client.RemoveNotebookFromFavoritesAsync(notebookId, CancellationToken.None).ConfigureAwait(false);
        else
            await _client.AddNotebookToFavoritesAsync(notebookId, folderId, CancellationToken.None).ConfigureAwait(false);
    }

    public Task SetNotebookFolderAsync(Guid notebookId, Guid? folderId)
        => _client.SetNotebookFolderAsync(notebookId, folderId, CancellationToken.None);

    public Task RemoveNotebookFromFavoritesAsync(Guid notebookId)
        => _client.RemoveNotebookFromFavoritesAsync(notebookId, CancellationToken.None);
}
