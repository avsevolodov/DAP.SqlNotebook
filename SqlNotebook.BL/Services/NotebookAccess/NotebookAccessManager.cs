using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DAP.SqlNotebook.BL.DataAccess;
using DAP.SqlNotebook.BL.DataAccess.Entities;
using DAP.SqlNotebook.Contract.Entities;

namespace DAP.SqlNotebook.BL.Services.NotebookAccess;

public sealed class NotebookAccessManager : INotebookAccessManager
{
    private readonly INotebookRepository _notebooks;
    private readonly IUserNotebookAccessRepository _access;

    public NotebookAccessManager(INotebookRepository notebooks, IUserNotebookAccessRepository access)
    {
        _notebooks = notebooks ?? throw new ArgumentNullException(nameof(notebooks));
        _access = access ?? throw new ArgumentNullException(nameof(access));
    }

    public async Task<bool> IsOwnerAsync(Guid notebookId, string? userLogin, CancellationToken ct = default)
    {
        if (notebookId == default || string.IsNullOrWhiteSpace(userLogin))
            return false;
        var n = await _notebooks.GetByIdAsync(notebookId, ct).ConfigureAwait(false);
        return n != null && !string.IsNullOrEmpty(n.CreatedBy) && string.Equals(n.CreatedBy, userLogin, StringComparison.OrdinalIgnoreCase);
    }

    public async Task<bool> CanViewAsync(Guid notebookId, string? userLogin, CancellationToken ct = default)
    {
        if (notebookId == default || string.IsNullOrWhiteSpace(userLogin))
            return false;

        if (await IsOwnerAsync(notebookId, userLogin, ct).ConfigureAwait(false))
            return true;

        var entry = await _access.GetAsync(notebookId, userLogin, ct).ConfigureAwait(false);
        return entry != null;
    }

    public async Task<bool> CanEditAsync(Guid notebookId, string? userLogin, CancellationToken ct = default)
    {
        if (notebookId == default || string.IsNullOrWhiteSpace(userLogin))
            return false;

        if (await IsOwnerAsync(notebookId, userLogin, ct).ConfigureAwait(false))
            return true;

        var entry = await _access.GetAsync(notebookId, userLogin, ct).ConfigureAwait(false);
        return entry != null && entry.Role == NotebookAccessRoleEntity.Editor;
    }

    public async Task<IReadOnlyList<NotebookAccessEntryInfo>> GetAccessAsync(Guid notebookId, string? requesterLogin, CancellationToken ct = default)
    {
        if (!await IsOwnerAsync(notebookId, requesterLogin, ct).ConfigureAwait(false))
            return Array.Empty<NotebookAccessEntryInfo>();

        var list = await _access.GetByNotebookIdAsync(notebookId, ct).ConfigureAwait(false);
        return list
            .Select(x => new NotebookAccessEntryInfo
            {
                UserLogin = x.UserLogin,
                Role = x.Role == NotebookAccessRoleEntity.Editor ? NotebookAccessRoleInfo.Editor : NotebookAccessRoleInfo.Viewer
            })
            .ToList();
    }

    public async Task SetAccessAsync(Guid notebookId, string? requesterLogin, IReadOnlyList<NotebookAccessEntryInfo> entries, CancellationToken ct = default)
    {
        if (!await IsOwnerAsync(notebookId, requesterLogin, ct).ConfigureAwait(false))
            throw new UnauthorizedAccessException("Only notebook owner can manage access.");

        var normalized = (entries ?? Array.Empty<NotebookAccessEntryInfo>())
            .Where(x => x != null && !string.IsNullOrWhiteSpace(x.UserLogin))
            .Select(x => new UserNotebookAccessEntity
            {
                NotebookId = notebookId,
                UserLogin = x.UserLogin.Trim(),
                Role = x.Role == NotebookAccessRoleInfo.Editor ? NotebookAccessRoleEntity.Editor : NotebookAccessRoleEntity.Viewer
            })
            .GroupBy(x => x.UserLogin, StringComparer.OrdinalIgnoreCase)
            .Select(g => g.First())
            .ToList();

        await _access.ReplaceAllAsync(notebookId, normalized, ct).ConfigureAwait(false);
    }

    public async Task RemoveAccessAsync(Guid notebookId, string? requesterLogin, string userLoginToRemove, CancellationToken ct = default)
    {
        if (!await IsOwnerAsync(notebookId, requesterLogin, ct).ConfigureAwait(false))
            throw new UnauthorizedAccessException("Only notebook owner can manage access.");

        userLoginToRemove ??= "";
        if (string.IsNullOrWhiteSpace(userLoginToRemove))
            return;

        await _access.DeleteAsync(notebookId, userLoginToRemove.Trim(), ct).ConfigureAwait(false);
    }
}

