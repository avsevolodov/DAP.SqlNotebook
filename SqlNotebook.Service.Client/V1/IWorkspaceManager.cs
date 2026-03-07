using DAP.SqlNotebook.Contract.Entities;

namespace DAP.SqlNotebook.Service.Client.V1;

public interface IWorkspaceManager
{
    /// <summary>Workspaces/folders owned by current user (for "My workspaces" page).</summary>
    Task<IReadOnlyList<WorkspaceInfo>> GetWorkspaces(CancellationToken ct);
    /// <summary>All nodes for the common workspace tree (hierarchy with folders).</summary>
    Task<IReadOnlyList<WorkspaceInfo>> GetTreeAsync(CancellationToken ct);
    Task<WorkspaceInfo?> GetWorkspace(Guid workspaceId, CancellationToken ct);
    Task<WorkspaceInfo> CreateWorkspace(WorkspaceInfo workspace, CancellationToken ct);
    Task<WorkspaceInfo> UpdateWorkspace(Guid id, WorkspaceInfo workspace, CancellationToken ct);
    Task DeleteWorkspace(Guid id, CancellationToken ct);
}
