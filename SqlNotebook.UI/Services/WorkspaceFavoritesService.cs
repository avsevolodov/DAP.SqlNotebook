using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DAP.SqlNotebook.Service.Client.V1;

namespace DAP.SqlNotebook.UI.Services;

/// <summary>Workspace favorites stored on the server for the current user.</summary>
public interface IWorkspaceFavoritesService
{
    Task<HashSet<Guid>> GetFavoriteIdsAsync();
    Task SetFavoriteIdsAsync(HashSet<Guid> ids);
    Task ToggleFavoriteAsync(Guid workspaceId);
}

public sealed class WorkspaceFavoritesService : IWorkspaceFavoritesService
{
    private readonly IWorkspaceManager _workspaceManager;

    public WorkspaceFavoritesService(IWorkspaceManager workspaceManager)
    {
        _workspaceManager = workspaceManager ?? throw new ArgumentNullException(nameof(workspaceManager));
    }

    public async Task<HashSet<Guid>> GetFavoriteIdsAsync()
    {
        var list = await _workspaceManager.GetFavoriteWorkspaceIdsAsync(CancellationToken.None).ConfigureAwait(false);
        return list != null ? new HashSet<Guid>(list) : new HashSet<Guid>();
    }

    public async Task SetFavoriteIdsAsync(HashSet<Guid> ids)
    {
        var target = ids ?? new HashSet<Guid>();
        var current = await GetFavoriteIdsAsync().ConfigureAwait(false);
        var ct = CancellationToken.None;
        foreach (var id in target.Where(id => !current.Contains(id)))
            await _workspaceManager.AddFavoriteAsync(id, ct).ConfigureAwait(false);
        foreach (var id in current.Where(id => !target.Contains(id)))
            await _workspaceManager.RemoveFavoriteAsync(id, ct).ConfigureAwait(false);
    }

    public async Task ToggleFavoriteAsync(Guid workspaceId)
    {
        var current = await GetFavoriteIdsAsync().ConfigureAwait(false);
        if (current.Contains(workspaceId))
            await _workspaceManager.RemoveFavoriteAsync(workspaceId, CancellationToken.None).ConfigureAwait(false);
        else
            await _workspaceManager.AddFavoriteAsync(workspaceId, CancellationToken.None).ConfigureAwait(false);
    }
}
