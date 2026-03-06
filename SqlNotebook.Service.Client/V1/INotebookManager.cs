using DAP.SqlNotebook.Contract.Entities;

namespace DAP.SqlNotebook.Service.Client.V1;

public interface INotebookManager
{
    Task<NotebooksResponse> GetNotebooks(
        CancellationToken ct,
        string? queryFilter = null,
        int offset = 0,
        int batchSize = 100,
        Guid? workspaceId = null);

    Task<NotebookInfo?> GetNotebook(Guid notebookId, CancellationToken ct);

    Task<NotebookInfo> CreateNotebook(NotebookInfo notebook, CancellationToken ct);

    Task<NotebookInfo> UpdateNotebook(Guid id, NotebookInfo notebook, CancellationToken ct);

    Task DeleteNotebook(Guid id, CancellationToken ct);

    Task<NotebookCellExecutionResultInfo> ExecuteQuery(Guid notebookId, string query, CancellationToken ct, int? commandTimeoutSeconds = null, Guid? catalogNodeId = null);
}