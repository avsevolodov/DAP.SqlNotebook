using DAP.SqlNotebook.Contract.Entities;

namespace DAP.SqlNotebook.Service.Client.V1;

public interface INotebookManager
{
    Task<NotebooksResponse> GetNotebooks(
        CancellationToken ct,
        string? queryFilter = null,
        int offset = 0,
        int batchSize = 100,
        Guid? workspaceId = null,
        NotebookStatusInfo? status = null);

    Task<NotebookInfo?> GetNotebook(Guid notebookId, CancellationToken ct);

    Task<NotebookInfo> CreateNotebook(NotebookInfo notebook, CancellationToken ct);

    Task<NotebookInfo> UpdateNotebook(Guid id, NotebookInfo notebook, CancellationToken ct);

    Task SetNotebookStatus(Guid id, NotebookStatusInfo status, CancellationToken ct);

    Task DeleteNotebook(Guid id, CancellationToken ct);

    Task<NotebookCellExecutionResultInfo> ExecuteQuery(Guid notebookId, string query, CancellationToken ct, int? commandTimeoutSeconds = null, Guid? catalogNodeId = null);

    Task<NotebookAccessResponse> GetNotebookAccess(Guid notebookId, CancellationToken ct);
    Task SetNotebookAccess(Guid notebookId, IReadOnlyList<NotebookAccessEntryInfo> entries, CancellationToken ct);
    Task RemoveNotebookAccess(Guid notebookId, string userLogin, CancellationToken ct);
}