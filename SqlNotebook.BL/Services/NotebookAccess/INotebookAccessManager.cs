using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using DAP.SqlNotebook.Contract.Entities;

namespace DAP.SqlNotebook.BL.Services.NotebookAccess;

public interface INotebookAccessManager
{
    Task<bool> CanViewAsync(Guid notebookId, string? userLogin, CancellationToken ct = default);
    Task<bool> CanEditAsync(Guid notebookId, string? userLogin, CancellationToken ct = default);
    Task<bool> IsOwnerAsync(Guid notebookId, string? userLogin, CancellationToken ct = default);

    Task<IReadOnlyList<NotebookAccessEntryInfo>> GetAccessAsync(Guid notebookId, string? requesterLogin, CancellationToken ct = default);
    Task SetAccessAsync(Guid notebookId, string? requesterLogin, IReadOnlyList<NotebookAccessEntryInfo> entries, CancellationToken ct = default);
    Task RemoveAccessAsync(Guid notebookId, string? requesterLogin, string userLoginToRemove, CancellationToken ct = default);
}

