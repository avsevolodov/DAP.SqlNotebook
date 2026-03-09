using System.Net.Http.Json;
using DAP.SqlNotebook.Contract.Entities;
using DAP.SqlNotebook.Service.Client.Exceptions;

namespace DAP.SqlNotebook.Service.Client.V1;

public class NotebookManager(HttpClient httpClient) : INotebookManager
{
    public async Task<NotebooksResponse> GetNotebooks(CancellationToken ct, string? queryFilter = null, int offset = 0, int batchSize = 100, Guid? workspaceId = null, NotebookStatusInfo? status = null)
    {
        var route = EndpointsHelper.CreateGetNotebooksRoute(offset, batchSize, queryFilter, workspaceId, status);
        using var responseMessage = await httpClient.GetAsync(route, ct);
        await responseMessage.ManagementServiceEnsureSuccessStatusCode();
        var response = await responseMessage.CdpReadContentAsAsync<NotebooksResponse>(ct);
        return response;
    }

    public async Task<NotebookInfo?> GetNotebook(Guid notebookId, CancellationToken ct)
    {
        var route = EndpointsHelper.GetNotebookByIdRoute(notebookId);
        using var responseMessage = await httpClient.GetAsync(route, ct);
        if (responseMessage.StatusCode == System.Net.HttpStatusCode.NotFound)
            return null;
        await responseMessage.ManagementServiceEnsureSuccessStatusCode();
        return await responseMessage.CdpReadContentAsAsync<NotebookInfo>(ct);
    }

    public async Task<NotebookInfo> CreateNotebook(NotebookInfo notebook, CancellationToken ct)
    {
        using var responseMessage = await httpClient.PostAsJsonAsync(EndpointsHelper.Notebooks, notebook, ct);
        await responseMessage.ManagementServiceEnsureSuccessStatusCode();
        var response = await responseMessage.CdpReadContentAsAsync<NotebookInfo>(ct);
        return response;
    }

    public async Task<NotebookInfo> UpdateNotebook(Guid id, NotebookInfo notebook, CancellationToken ct)
    {
        var route = EndpointsHelper.GetNotebookByIdRoute(id);
        using var responseMessage = await httpClient.PutAsJsonAsync(route, notebook, ct);
        await responseMessage.ManagementServiceEnsureSuccessStatusCode();
        var response = await responseMessage.CdpReadContentAsAsync<NotebookInfo>(ct);
        return response;
    }

    public async Task SetNotebookStatus(Guid id, NotebookStatusInfo status, CancellationToken ct)
    {
        var route = EndpointsHelper.SetNotebookStatusRoute(id);
        var body = new SetNotebookStatusRequest { Status = status };
        using var responseMessage = await httpClient.PatchAsJsonAsync(route, body, ct);
        await responseMessage.ManagementServiceEnsureSuccessStatusCode();
    }

    public async Task DeleteNotebook(Guid id, CancellationToken ct)
    {
        var route = EndpointsHelper.GetNotebookByIdRoute(id);
        using var responseMessage = await httpClient.DeleteAsync(route, ct);
        await responseMessage.ManagementServiceEnsureSuccessStatusCode();
    }

    public async Task<NotebookCellExecutionResultInfo> ExecuteQuery(Guid notebookId, string query, CancellationToken ct, int? commandTimeoutSeconds = null, Guid? catalogNodeId = null)
    {
        var route = EndpointsHelper.GetNotebookExecuteRoute(notebookId);
        var payload = new ConnectionExecutionRequest { Query = query, CommandTimeoutSeconds = commandTimeoutSeconds, CatalogNodeId = catalogNodeId };

        using var responseMessage = await httpClient.PostAsJsonAsync(route, payload, ct);
        await responseMessage.ManagementServiceEnsureSuccessStatusCode();
        var response = await responseMessage.CdpReadContentAsAsync<NotebookCellExecutionResultInfo>(ct);
        return response;
    }

    public async Task<NotebookAccessResponse> GetNotebookAccess(Guid notebookId, CancellationToken ct)
    {
        var route = EndpointsHelper.NotebookAccessRoute(notebookId);
        using var responseMessage = await httpClient.GetAsync(route, ct);
        await responseMessage.ManagementServiceEnsureSuccessStatusCode();
        return await responseMessage.CdpReadContentAsAsync<NotebookAccessResponse>(ct);
    }

    public async Task SetNotebookAccess(Guid notebookId, IReadOnlyList<NotebookAccessEntryInfo> entries, CancellationToken ct)
    {
        var route = EndpointsHelper.NotebookAccessRoute(notebookId);
        var body = new SetNotebookAccessRequest { Entries = entries?.ToList() ?? new List<NotebookAccessEntryInfo>() };
        using var responseMessage = await httpClient.PutAsJsonAsync(route, body, ct);
        await responseMessage.ManagementServiceEnsureSuccessStatusCode();
    }

    public async Task RemoveNotebookAccess(Guid notebookId, string userLogin, CancellationToken ct)
    {
        var route = EndpointsHelper.NotebookAccessEntryRoute(notebookId, userLogin);
        using var responseMessage = await httpClient.DeleteAsync(route, ct);
        await responseMessage.ManagementServiceEnsureSuccessStatusCode();
    }
}
