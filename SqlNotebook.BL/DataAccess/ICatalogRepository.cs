using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using DAP.SqlNotebook.BL.Models;

namespace DAP.SqlNotebook.BL.DataAccess;

public interface ICatalogRepository
{
    Task<IReadOnlyList<CatalogNode>> GetNodesAsync(Guid? parentId, CancellationToken ct = default);
    /// <summary>Returns catalog nodes of type Database (for separate schema API: list DBs).</summary>
    Task<IReadOnlyList<CatalogNode>> GetDatabaseNodesAsync(CancellationToken ct = default);
    Task<CatalogNode?> GetNodeByIdAsync(Guid id, CancellationToken ct = default);
    Task<CatalogNode> CreateNodeAsync(CreateCatalogNodeParams create, CancellationToken ct = default);
    Task<CatalogNode?> UpdateNodeAsync(Guid id, UpdateCatalogNodeParams update, CancellationToken ct = default);
    Task<bool> DeleteNodeAsync(Guid id, CancellationToken ct = default);
    Task<string?> GetEntitySelectTextAsync(Guid entityId, int top = 10, CancellationToken ct = default);
    Task EnsureCatalogRootAsync(CancellationToken ct = default);
    Task<IReadOnlyList<DbEntityInfo>> GetEntitiesAsync(CancellationToken ct = default);
    /// <summary>Returns entities (tables/views) that belong to the given source (database node), with paging.</summary>
    Task<(IReadOnlyList<DbEntityInfo> Items, int TotalCount)> GetEntitiesBySourceNodePagedAsync(Guid catalogNodeId, int offset, int count, CancellationToken ct = default);
    /// <summary>Returns entities (tables/views) that belong to the given source (database node).</summary>
    Task<IReadOnlyList<DbEntityInfo>> GetEntitiesBySourceNodeAsync(Guid catalogNodeId, CancellationToken ct = default);
    Task<DbEntityInfo?> GetEntityByIdAsync(Guid entityId, CancellationToken ct = default);
    Task<DbEntityInfo> CreateEntityAsync(CreateDbEntityParams create, CancellationToken ct = default);
    Task<DbEntityInfo?> UpdateEntityAsync(Guid entityId, UpdateDbEntityParams update, CancellationToken ct = default);
    Task<bool> DeleteEntityAsync(Guid entityId, CancellationToken ct = default);
    Task<IReadOnlyList<DbFieldInfo>> GetFieldsAsync(Guid entityId, CancellationToken ct = default);
    Task<DbFieldInfo> CreateFieldAsync(CreateDbFieldParams create, CancellationToken ct = default);
    Task<DbFieldInfo?> UpdateFieldAsync(Guid fieldId, UpdateDbFieldParams update, CancellationToken ct = default);
    Task<bool> DeleteFieldAsync(Guid fieldId, CancellationToken ct = default);
}
