using DAP.SqlNotebook.Contract.Entities;

namespace DAP.SqlNotebook.Service.Client.V1;

public interface ICatalogManager
{
    Task<IReadOnlyList<CatalogNodeInfo>> GetNodesAsync(Guid? parentId, CancellationToken ct);
    Task<IReadOnlyList<CatalogNodeInfo>> GetDatabasesAsync(CancellationToken ct);
    Task<CatalogNodeInfo?> GetNodeAsync(Guid id, CancellationToken ct);
    Task<CatalogNodeInfo> CreateNodeAsync(CatalogNodeCreateInfo model, CancellationToken ct);
    Task<CatalogNodeInfo> UpdateNodeAsync(Guid id, CatalogNodeUpdateInfo model, CancellationToken ct);
    Task DeleteNodeAsync(Guid id, CancellationToken ct);
    Task<ConnectionHealthInfo> GetConnectionStatusAsync(Guid nodeId, CancellationToken ct);
    Task<SchemaImportResultInfo> ImportStructureAsync(Guid nodeId, CancellationToken ct);
    Task<string?> GetEntitySelectTextAsync(Guid entityId, int? top, CancellationToken ct);

    Task<IReadOnlyList<DbEntityInfo>> GetEntitiesAsync(Guid? nodeId, CancellationToken ct);
    Task<EntitiesPageResult> GetEntitiesPagedAsync(Guid nodeId, int offset, int count, CancellationToken ct);
    Task<DbEntityInfo?> GetEntityAsync(Guid entityId, CancellationToken ct);
    Task<DbEntityInfo> CreateEntityAsync(CreateDbEntityInfo model, CancellationToken ct);
    Task<DbEntityInfo> UpdateEntityAsync(Guid entityId, UpdateDbEntityInfo model, CancellationToken ct);
    Task DeleteEntityAsync(Guid entityId, CancellationToken ct);

    Task<IReadOnlyList<DbFieldInfo>> GetFieldsAsync(Guid entityId, CancellationToken ct);
    Task<DbFieldInfo> CreateFieldAsync(CreateDbFieldInfo model, CancellationToken ct);
    Task<DbFieldInfo> UpdateFieldAsync(Guid fieldId, UpdateDbFieldInfo model, CancellationToken ct);
    Task DeleteFieldAsync(Guid fieldId, CancellationToken ct);
}
