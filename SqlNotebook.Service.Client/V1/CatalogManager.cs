using System.Net.Http.Json;
using DAP.SqlNotebook.Contract.Entities;
using DAP.SqlNotebook.Service.Client;
using DAP.SqlNotebook.Service.Client.Exceptions;

namespace DAP.SqlNotebook.Service.Client.V1;

public class CatalogManager : ICatalogManager
{
    private readonly HttpClient _httpClient;

    public CatalogManager(HttpClient httpClient)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
    }

    public async Task<IReadOnlyList<CatalogNodeInfo>> GetNodesAsync(Guid? parentId, CancellationToken ct)
    {
        var route = EndpointsHelper.CatalogNodes(parentId);
        var list = await _httpClient.GetFromJsonAsync<List<CatalogNodeInfo>>(route, ct);
        return list ?? new List<CatalogNodeInfo>();
    }

    public async Task<IReadOnlyList<CatalogNodeInfo>> GetDatabasesAsync(CancellationToken ct)
    {
        var list = await _httpClient.GetFromJsonAsync<List<CatalogNodeInfo>>(EndpointsHelper.CatalogDatabases(), ct);
        return list ?? new List<CatalogNodeInfo>();
    }

    public async Task<CatalogNodeInfo?> GetNodeAsync(Guid id, CancellationToken ct)
    {
        var route = EndpointsHelper.CatalogNode(id);
        using var response = await _httpClient.GetAsync(route, ct);
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            return null;
        await response.ManagementServiceEnsureSuccessStatusCode();
        return await response.CdpReadContentAsAsync<CatalogNodeInfo>(ct);
    }

    public async Task<CatalogNodeInfo> CreateNodeAsync(CatalogNodeCreateInfo model, CancellationToken ct)
    {
        using var response = await _httpClient.PostAsJsonAsync(EndpointsHelper.CatalogNodesCreate(), model, ct);
        await response.ManagementServiceEnsureSuccessStatusCode();
        return await response.CdpReadContentAsAsync<CatalogNodeInfo>(ct);
    }

    public async Task<CatalogNodeInfo> UpdateNodeAsync(Guid id, CatalogNodeUpdateInfo model, CancellationToken ct)
    {
        var route = EndpointsHelper.CatalogNode(id);
        using var response = await _httpClient.PutAsJsonAsync(route, model, ct);
        await response.ManagementServiceEnsureSuccessStatusCode();
        return await response.CdpReadContentAsAsync<CatalogNodeInfo>(ct);
    }

    public async Task DeleteNodeAsync(Guid id, CancellationToken ct)
    {
        var route = EndpointsHelper.CatalogNode(id);
        using var response = await _httpClient.DeleteAsync(route, ct);
        await response.ManagementServiceEnsureSuccessStatusCode();
    }

    public async Task<ConnectionHealthInfo> GetConnectionStatusAsync(Guid nodeId, CancellationToken ct)
    {
        var route = EndpointsHelper.CatalogConnectionStatus(nodeId);
        using var response = await _httpClient.GetAsync(route, ct);
        await response.ManagementServiceEnsureSuccessStatusCode();
        return await response.CdpReadContentAsAsync<ConnectionHealthInfo>(ct);
    }

    public async Task<SchemaImportResultInfo> ImportStructureAsync(Guid nodeId, CancellationToken ct)
    {
        var route = EndpointsHelper.CatalogImportStructure(nodeId);
        using var response = await _httpClient.PostAsync(route, null, ct);
        await response.ManagementServiceEnsureSuccessStatusCode();
        return await response.CdpReadContentAsAsync<SchemaImportResultInfo>(ct);
    }

    public async Task<string?> GetEntitySelectTextAsync(Guid entityId, int? top, CancellationToken ct)
    {
        var route = EndpointsHelper.EntitySelectText(entityId, top);
        using var response = await _httpClient.GetAsync(route, ct);
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            return null;
        await response.ManagementServiceEnsureSuccessStatusCode();
        return await response.CdpReadContentAsAsync<string>(ct);
    }

    public async Task<IReadOnlyList<DbEntityInfo>> GetEntitiesAsync(Guid? nodeId, CancellationToken ct)
    {
        var route = EndpointsHelper.CatalogEntities(nodeId);
        var list = await _httpClient.GetFromJsonAsync<List<DbEntityInfo>>(route, ct);
        return list ?? new List<DbEntityInfo>();
    }

    public async Task<EntitiesPageResult> GetEntitiesPagedAsync(Guid nodeId, int offset, int count, CancellationToken ct)
    {
        var route = EndpointsHelper.CatalogEntities(nodeId, offset, count);
        using var response = await _httpClient.GetAsync(route, ct);
        await response.ManagementServiceEnsureSuccessStatusCode();
        return await response.CdpReadContentAsAsync<EntitiesPageResult>(ct);
    }

    public async Task<DbEntityInfo?> GetEntityAsync(Guid entityId, CancellationToken ct)
    {
        var route = EndpointsHelper.CatalogEntity(entityId);
        using var response = await _httpClient.GetAsync(route, ct);
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            return null;
        await response.ManagementServiceEnsureSuccessStatusCode();
        return await response.CdpReadContentAsAsync<DbEntityInfo>(ct);
    }

    public async Task<DbEntityInfo> CreateEntityAsync(CreateDbEntityInfo model, CancellationToken ct)
    {
        var route = $"{EndpointsHelper.Catalog}/entities";
        using var response = await _httpClient.PostAsJsonAsync(route, model, ct);
        await response.ManagementServiceEnsureSuccessStatusCode();
        return await response.CdpReadContentAsAsync<DbEntityInfo>(ct);
    }

    public async Task<DbEntityInfo> UpdateEntityAsync(Guid entityId, UpdateDbEntityInfo model, CancellationToken ct)
    {
        var route = EndpointsHelper.CatalogEntity(entityId);
        using var response = await _httpClient.PutAsJsonAsync(route, model, ct);
        await response.ManagementServiceEnsureSuccessStatusCode();
        return await response.CdpReadContentAsAsync<DbEntityInfo>(ct);
    }

    public async Task DeleteEntityAsync(Guid entityId, CancellationToken ct)
    {
        var route = EndpointsHelper.CatalogEntity(entityId);
        using var response = await _httpClient.DeleteAsync(route, ct);
        await response.ManagementServiceEnsureSuccessStatusCode();
    }

    public async Task<IReadOnlyList<DbFieldInfo>> GetFieldsAsync(Guid entityId, CancellationToken ct)
    {
        var route = EndpointsHelper.CatalogEntityFields(entityId);
        var list = await _httpClient.GetFromJsonAsync<List<DbFieldInfo>>(route, ct);
        return list ?? new List<DbFieldInfo>();
    }

    public async Task<DbFieldInfo> CreateFieldAsync(CreateDbFieldInfo model, CancellationToken ct)
    {
        var route = $"{EndpointsHelper.Catalog}/fields";
        using var response = await _httpClient.PostAsJsonAsync(route, model, ct);
        await response.ManagementServiceEnsureSuccessStatusCode();
        return await response.CdpReadContentAsAsync<DbFieldInfo>(ct);
    }

    public async Task<DbFieldInfo> UpdateFieldAsync(Guid fieldId, UpdateDbFieldInfo model, CancellationToken ct)
    {
        var route = EndpointsHelper.CatalogField(fieldId);
        using var response = await _httpClient.PutAsJsonAsync(route, model, ct);
        await response.ManagementServiceEnsureSuccessStatusCode();
        return await response.CdpReadContentAsAsync<DbFieldInfo>(ct);
    }

    public async Task DeleteFieldAsync(Guid fieldId, CancellationToken ct)
    {
        var route = EndpointsHelper.CatalogField(fieldId);
        using var response = await _httpClient.DeleteAsync(route, ct);
        await response.ManagementServiceEnsureSuccessStatusCode();
    }
}
