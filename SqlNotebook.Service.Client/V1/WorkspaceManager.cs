using System.Net.Http.Json;
using DAP.SqlNotebook.Contract.Entities;
using DAP.SqlNotebook.Service.Client;
using DAP.SqlNotebook.Service.Client.Exceptions;

namespace DAP.SqlNotebook.Service.Client.V1;

public class WorkspaceManager : IWorkspaceManager
{
    private readonly HttpClient _httpClient;

    public WorkspaceManager(HttpClient httpClient)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
    }

    public async Task<IReadOnlyList<WorkspaceInfo>> GetWorkspaces(CancellationToken ct)
    {
        var list = await _httpClient.GetFromJsonAsync<List<WorkspaceInfo>>(
            EndpointsHelper.Workspaces, ct);
        return list ?? new List<WorkspaceInfo>();
    }

    public async Task<IReadOnlyList<WorkspaceInfo>> GetTreeAsync(CancellationToken ct)
    {
        var list = await _httpClient.GetFromJsonAsync<List<WorkspaceInfo>>(
            $"{EndpointsHelper.Workspaces}/tree", ct);
        return list ?? new List<WorkspaceInfo>();
    }

    public async Task<WorkspaceInfo?> GetWorkspace(Guid workspaceId, CancellationToken ct)
    {
        var route = $"{EndpointsHelper.Workspaces}/{workspaceId:N}";
        using var response = await _httpClient.GetAsync(route, ct);
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            return null;
        await response.ManagementServiceEnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<WorkspaceInfo>(cancellationToken: ct);
    }

    public async Task<WorkspaceInfo> CreateWorkspace(WorkspaceInfo workspace, CancellationToken ct)
    {
        using var response = await _httpClient.PostAsJsonAsync(EndpointsHelper.Workspaces, workspace, ct);
        await response.ManagementServiceEnsureSuccessStatusCode();
        var created = await response.Content.ReadFromJsonAsync<WorkspaceInfo>(cancellationToken: ct);
        return created ?? workspace;
    }

    public async Task<WorkspaceInfo> UpdateWorkspace(Guid id, WorkspaceInfo workspace, CancellationToken ct)
    {
        var route = $"{EndpointsHelper.Workspaces}/{id:N}";
        using var response = await _httpClient.PutAsJsonAsync(route, workspace, ct);
        await response.ManagementServiceEnsureSuccessStatusCode();
        var updated = await response.Content.ReadFromJsonAsync<WorkspaceInfo>(cancellationToken: ct);
        return updated ?? workspace;
    }

    public async Task DeleteWorkspace(Guid id, CancellationToken ct)
    {
        var route = $"{EndpointsHelper.Workspaces}/{id:N}";
        using var response = await _httpClient.DeleteAsync(route, ct);
        await response.ManagementServiceEnsureSuccessStatusCode();
    }
}
