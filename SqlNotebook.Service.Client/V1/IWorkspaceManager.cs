using DAP.SqlNotebook.Contract.Entities;

namespace DAP.SqlNotebook.Service.Client.V1;

public interface IWorkspaceManager
{
    Task<IReadOnlyList<WorkspaceInfo>> GetWorkspaces(CancellationToken ct);
    Task<WorkspaceInfo?> GetWorkspace(Guid workspaceId, CancellationToken ct);
    Task<WorkspaceInfo> CreateWorkspace(WorkspaceInfo workspace, CancellationToken ct);
    Task<WorkspaceInfo> UpdateWorkspace(Guid id, WorkspaceInfo workspace, CancellationToken ct);
    Task DeleteWorkspace(Guid id, CancellationToken ct);
}
