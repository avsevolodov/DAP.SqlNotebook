using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using DAP.SqlNotebook.Contract.Entities;

namespace DAP.SqlNotebook.BL.Services.Notebook;

/// <summary>
/// Business logic for notebooks: CRUD and query execution. No direct DB access from controllers.
/// </summary>
public interface INotebookManager
{
    Task<(IReadOnlyList<NotebookMetaInfo> Notebooks, int TotalCount)> GetListAsync(
        int offset,
        int batchSize,
        Guid? workspaceId,
        string? userLogin,
        NotebookStatusInfo? status = null,
        CancellationToken ct = default);

    Task<NotebookInfo?> GetByIdAsync(Guid id, CancellationToken ct);

    Task<NotebookInfo> CreateAsync(NotebookInfo model, string? userLogin, CancellationToken ct);

    Task<NotebookInfo> UpdateAsync(Guid id, NotebookInfo model, string? userLogin, CancellationToken ct);

    Task SetStatusAsync(Guid id, NotebookStatusInfo status, CancellationToken ct = default);

    Task DeleteAsync(Guid id, CancellationToken ct);

    Task<NotebookCellExecutionResultInfo> ExecuteQueryAsync(string query, int timeoutSeconds, CancellationToken ct);
}
