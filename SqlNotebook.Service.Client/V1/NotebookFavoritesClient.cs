using System.Net.Http.Json;
using DAP.SqlNotebook.Contract.Entities;
using DAP.SqlNotebook.Service.Client;
using DAP.SqlNotebook.Service.Client.Exceptions;

namespace DAP.SqlNotebook.Service.Client.V1;

public class NotebookFavoritesClient : INotebookFavoritesClient
{
    private readonly HttpClient _httpClient;

    public NotebookFavoritesClient(HttpClient httpClient)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
    }

    public async Task<IReadOnlyList<FavoriteFolderInfo>> GetFoldersAsync(CancellationToken ct)
    {
        var list = await _httpClient.GetFromJsonAsync<List<FavoriteFolderInfo>>(
            EndpointsHelper.FavoritesFolders(), ct);
        return list ?? new List<FavoriteFolderInfo>();
    }

    public async Task<FavoriteFolderInfo> CreateFolderAsync(string name, Guid? parentId, CancellationToken ct)
    {
        var body = new { Name = name, ParentId = parentId };
        using var response = await _httpClient.PostAsJsonAsync(EndpointsHelper.FavoritesFolders(), body, ct);
        await response.ManagementServiceEnsureSuccessStatusCode();
        var created = await response.Content.ReadFromJsonAsync<FavoriteFolderInfo>(cancellationToken: ct);
        return created!;
    }

    public async Task DeleteFolderAsync(Guid folderId, CancellationToken ct)
    {
        var route = $"{EndpointsHelper.Favorites}/folders/{folderId:N}";
        using var response = await _httpClient.DeleteAsync(route, ct);
        await response.ManagementServiceEnsureSuccessStatusCode();
    }

    public async Task<IReadOnlyList<FavoriteNotebookItemInfo>> GetFavoriteNotebooksAsync(CancellationToken ct)
    {
        var list = await _httpClient.GetFromJsonAsync<List<FavoriteNotebookItemInfo>>(
            EndpointsHelper.FavoritesNotebooks(), ct);
        return list ?? new List<FavoriteNotebookItemInfo>();
    }

    public async Task AddNotebookToFavoritesAsync(Guid notebookId, Guid? folderId, CancellationToken ct)
    {
        var body = new { FolderId = folderId };
        using var response = await _httpClient.PostAsJsonAsync(
            EndpointsHelper.FavoritesNotebook(notebookId), body, ct);
        await response.ManagementServiceEnsureSuccessStatusCode();
    }

    public async Task RemoveNotebookFromFavoritesAsync(Guid notebookId, CancellationToken ct)
    {
        using var response = await _httpClient.DeleteAsync(EndpointsHelper.FavoritesNotebook(notebookId), ct);
        await response.ManagementServiceEnsureSuccessStatusCode();
    }

    public async Task SetNotebookFolderAsync(Guid notebookId, Guid? folderId, CancellationToken ct)
    {
        var body = new { FolderId = folderId };
        using var response = await _httpClient.PutAsJsonAsync(
            EndpointsHelper.FavoritesNotebookFolder(notebookId), body, ct);
        await response.ManagementServiceEnsureSuccessStatusCode();
    }

    public async Task<HashSet<Guid>> GetFavoriteNotebookIdsAsync(CancellationToken ct)
    {
        var list = await _httpClient.GetFromJsonAsync<List<Guid>>(
            EndpointsHelper.FavoritesNotebookIds(), ct);
        return list != null ? new HashSet<Guid>(list) : new HashSet<Guid>();
    }
}
