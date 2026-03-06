using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DAP.SqlNotebook.BL.DataAccess.Entities;
using DAP.SqlNotebook.BL.Models;
using Microsoft.EntityFrameworkCore;

namespace DAP.SqlNotebook.BL.DataAccess;

public class CatalogRepository : ICatalogRepository
{
    private readonly SqlNotebookDbContext _db;

    public CatalogRepository(SqlNotebookDbContext db)
    {
        _db = db ?? throw new ArgumentNullException(nameof(db));
    }

    public async Task<IReadOnlyList<CatalogNode>> GetNodesAsync(Guid? parentId, CancellationToken ct = default)
    {
        if (!parentId.HasValue)
        {
            var anyRoot = await _db.DataMartNodes.AnyAsync(n => n.ParentId == null, ct).ConfigureAwait(false);
            if (!anyRoot)
                await EnsureCatalogRootAsync(ct).ConfigureAwait(false);
        }

        var nodes = await _db.DataMartNodes
            .AsNoTracking()
            .Where(n => n.ParentId == parentId)
            .OrderBy(n => n.SortOrder)
            .ThenBy(n => n.Name)
            .ToListAsync(ct)
            .ConfigureAwait(false);

        var ids = nodes.Select(n => n.Id).ToList();
        var parentIdsWithChildren = ids.Count == 0
            ? new HashSet<Guid>()
            : (await _db.DataMartNodes
                .AsNoTracking()
                .Where(n => n.ParentId != null && ids.Contains(n.ParentId.Value))
                .Select(n => n.ParentId!.Value)
                .Distinct()
                .ToListAsync(ct)
                .ConfigureAwait(false)).ToHashSet();

        var result = new List<CatalogNode>(nodes.Count);
        foreach (var n in nodes)
        {
            string? qualifiedName = null;
            if (n.EntityId.HasValue)
            {
                qualifiedName = await _db.DbEntities
                    .AsNoTracking()
                    .Where(e => e.Id == n.EntityId.Value)
                    .Select(e => e.Name)
                    .FirstOrDefaultAsync(ct)
                    .ConfigureAwait(false);
            }
            result.Add(new CatalogNode
            {
                Id = n.Id,
                ParentId = n.ParentId,
                Type = n.Type.ToString(),
                Name = n.Name,
                Description = n.Description,
                Owner = n.Owner,
                Provider = n.Provider,
                ConnectionInfo = n.ConnectionInfo,
                DatabaseName = n.DatabaseName,
                AuthType = n.AuthType,
                Login = n.Login,
                PasswordEncrypted = n.PasswordEncrypted,
                HasChildren = parentIdsWithChildren.Contains(n.Id),
                EntityId = n.EntityId,
                QualifiedName = qualifiedName,
            });
        }
        return result;
    }

    public async Task<IReadOnlyList<CatalogNode>> GetDatabaseNodesAsync(CancellationToken ct = default)
    {
        var nodes = await _db.DataMartNodes
            .AsNoTracking()
            .Where(n => n.Type == DataMartNodeType.Database)
            .OrderBy(n => n.SortOrder)
            .ThenBy(n => n.Name)
            .ToListAsync(ct)
            .ConfigureAwait(false);
        var result = new List<CatalogNode>(nodes.Count);
        foreach (var n in nodes)
        {
            string? qualifiedName = null;
            if (n.EntityId.HasValue)
            {
                qualifiedName = await _db.DbEntities
                    .AsNoTracking()
                    .Where(e => e.Id == n.EntityId.Value)
                    .Select(e => e.Name)
                    .FirstOrDefaultAsync(ct)
                    .ConfigureAwait(false);
            }
            result.Add(new CatalogNode
            {
                Id = n.Id,
                ParentId = n.ParentId,
                Type = n.Type.ToString(),
                Name = n.Name,
                Description = n.Description,
                Owner = n.Owner,
                Provider = n.Provider,
                ConnectionInfo = n.ConnectionInfo,
                DatabaseName = n.DatabaseName,
                AuthType = n.AuthType,
                Login = n.Login,
                PasswordEncrypted = n.PasswordEncrypted,
                HasChildren = await _db.DataMartNodes.AnyAsync(x => x.ParentId == n.Id, ct).ConfigureAwait(false),
                EntityId = n.EntityId,
                QualifiedName = qualifiedName,
            });
        }
        return result;
    }

    public async Task<CatalogNode?> GetNodeByIdAsync(Guid id, CancellationToken ct = default)
    {
        var n = await _db.DataMartNodes
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id, ct)
            .ConfigureAwait(false);
        if (n == null) return null;

        string? qualifiedName = null;
        if (n.EntityId.HasValue)
        {
            qualifiedName = await _db.DbEntities
                .AsNoTracking()
                .Where(e => e.Id == n.EntityId.Value)
                .Select(e => e.Name)
                .FirstOrDefaultAsync(ct)
                .ConfigureAwait(false);
        }
        var hasChildren = await _db.DataMartNodes.AnyAsync(x => x.ParentId == id, ct).ConfigureAwait(false);
        return new CatalogNode
        {
            Id = n.Id,
            ParentId = n.ParentId,
            Type = n.Type.ToString(),
            Name = n.Name,
            Description = n.Description,
            Owner = n.Owner,
            Provider = n.Provider,
            ConnectionInfo = n.ConnectionInfo,
            DatabaseName = n.DatabaseName,
            AuthType = n.AuthType,
            Login = n.Login,
            PasswordEncrypted = n.PasswordEncrypted,
            ConsumerGroupPrefix = n.ConsumerGroupPrefix,
            ConsumerGroupAutoGenerate = n.ConsumerGroupAutoGenerate,
            HasChildren = hasChildren,
            EntityId = n.EntityId,
            QualifiedName = qualifiedName,
        };
    }

    public async Task<CatalogNode> CreateNodeAsync(CreateCatalogNodeParams create, CancellationToken ct = default)
    {
        var node = new DataMartNodeEntity
        {
            Id = Guid.NewGuid(),
            ParentId = create.ParentId,
            Type = (DataMartNodeType)Math.Clamp(create.Type, 0, 3),
            Name = create.Name.Trim(),
            Description = create.Description?.Trim(),
            Owner = create.Owner?.Trim(),
            Provider = create.Provider?.Trim(),
            ConnectionInfo = create.ConnectionInfo?.Trim(),
            DatabaseName = create.DatabaseName?.Trim(),
            AuthType = create.AuthType?.Trim(),
            Login = create.Login?.Trim(),
            PasswordEncrypted = create.PasswordEncrypted,
            ConsumerGroupPrefix = create.ConsumerGroupPrefix?.Trim(),
            ConsumerGroupAutoGenerate = create.ConsumerGroupAutoGenerate,
            SortOrder = 0,
            EntityId = create.EntityId,
        };
        _db.DataMartNodes.Add(node);
        await _db.SaveChangesAsync(ct).ConfigureAwait(false);
        return (await GetNodeByIdAsync(node.Id, ct).ConfigureAwait(false))!;
    }

    public async Task<CatalogNode?> UpdateNodeAsync(Guid id, UpdateCatalogNodeParams update, CancellationToken ct = default)
    {
        var node = await _db.DataMartNodes.FirstOrDefaultAsync(x => x.Id == id, ct).ConfigureAwait(false);
        if (node == null) return null;
        if (update.Name != null) node.Name = update.Name.Trim();
        if (update.Description != null) node.Description = update.Description.Trim();
        if (update.Owner != null) node.Owner = update.Owner.Trim();
        if (update.Provider != null) node.Provider = update.Provider.Trim();
        if (update.ConnectionInfo != null) node.ConnectionInfo = update.ConnectionInfo.Trim();
        if (update.DatabaseName != null) node.DatabaseName = update.DatabaseName.Trim();
        if (update.AuthType != null) node.AuthType = update.AuthType.Trim();
        if (update.Login != null) node.Login = update.Login.Trim();
        if (update.PasswordEncrypted != null) node.PasswordEncrypted = update.PasswordEncrypted;
        if (update.ConsumerGroupPrefix != null) node.ConsumerGroupPrefix = update.ConsumerGroupPrefix.Trim();
        if (update.ConsumerGroupAutoGenerate.HasValue) node.ConsumerGroupAutoGenerate = update.ConsumerGroupAutoGenerate.Value;
        if (update.AuthType != null && !string.Equals(update.AuthType.Trim(), "Basic", StringComparison.OrdinalIgnoreCase))
            node.PasswordEncrypted = null;
        await _db.SaveChangesAsync(ct).ConfigureAwait(false);
        return await GetNodeByIdAsync(id, ct).ConfigureAwait(false);
    }

    public async Task<bool> DeleteNodeAsync(Guid id, CancellationToken ct = default)
    {
        var node = await _db.DataMartNodes.FirstOrDefaultAsync(x => x.Id == id, ct).ConfigureAwait(false);
        if (node == null) return false;
        await DeleteNodeRecursiveAsync(id, ct).ConfigureAwait(false);
        return true;
    }

    private async Task DeleteNodeRecursiveAsync(Guid id, CancellationToken ct)
    {
        var children = await _db.DataMartNodes.Where(n => n.ParentId == id).Select(n => n.Id).ToListAsync(ct).ConfigureAwait(false);
        foreach (var childId in children)
            await DeleteNodeRecursiveAsync(childId, ct).ConfigureAwait(false);
        var node = await _db.DataMartNodes.FirstOrDefaultAsync(x => x.Id == id, ct).ConfigureAwait(false);
        if (node != null)
        {
            _db.DataMartNodes.Remove(node);
            await _db.SaveChangesAsync(ct).ConfigureAwait(false);
        }
    }

    public async Task<string?> GetEntitySelectTextAsync(Guid entityId, int top = 10, CancellationToken ct = default)
    {
        var entity = await _db.DbEntities
            .AsNoTracking()
            .Where(e => e.Id == entityId)
            .Select(e => new { e.Name })
            .FirstOrDefaultAsync(ct)
            .ConfigureAwait(false);
        if (entity == null) return null;
        var n = Math.Clamp(top, 1, 10000);
        var fields = await _db.DbFields
            .AsNoTracking()
            .Where(f => f.EntityId == entityId)
            .OrderBy(f => f.Name)
            .Select(f => f.Name)
            .ToListAsync(ct)
            .ConfigureAwait(false);
        if (fields.Count == 0)
            return $"SELECT TOP {n} * FROM {entity.Name}";
        return $"SELECT TOP {n} {string.Join(", ", fields)}\nFROM {entity.Name}";
    }

    public async Task EnsureCatalogRootAsync(CancellationToken ct = default)
    {
        try
        {
            var root = new DataMartNodeEntity
            {
                Id = Guid.NewGuid(),
                ParentId = null,
                Type = DataMartNodeType.Database,
                Name = "SqlNotebook",
                Description = "Main SqlNotebook database",
                SortOrder = 0,
                Provider = "MSSQL"
            };
            _db.DataMartNodes.Add(root);
            await _db.SaveChangesAsync(ct).ConfigureAwait(false);
        }
        catch
        {
            // Table may not exist; ignore
        }
    }

    public async Task<IReadOnlyList<DbEntityInfo>> GetEntitiesAsync(CancellationToken ct = default)
    {
        return await _db.DbEntities
            .AsNoTracking()
            .OrderBy(e => e.Name)
            .Select(e => new DbEntityInfo
            {
                Id = e.Id,
                Name = e.Name,
                DisplayName = e.DisplayName,
                Description = e.Description,
            })
            .ToListAsync(ct)
            .ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<DbEntityInfo>> GetEntitiesBySourceNodeAsync(Guid catalogNodeId, CancellationToken ct = default)
    {
        var entityIds = await _db.DataMartNodes
            .AsNoTracking()
            .Where(n => n.ParentId == catalogNodeId && n.EntityId != null)
            .Select(n => n.EntityId!.Value)
            .Distinct()
            .ToListAsync(ct)
            .ConfigureAwait(false);
        if (entityIds.Count == 0)
            return new List<DbEntityInfo>();
        return await _db.DbEntities
            .AsNoTracking()
            .Where(e => entityIds.Contains(e.Id))
            .OrderBy(e => e.Name)
            .Select(e => new DbEntityInfo
            {
                Id = e.Id,
                Name = e.Name,
                DisplayName = e.DisplayName,
                Description = e.Description,
            })
            .ToListAsync(ct)
            .ConfigureAwait(false);
    }

    public async Task<(IReadOnlyList<DbEntityInfo> Items, int TotalCount)> GetEntitiesBySourceNodePagedAsync(Guid catalogNodeId, int offset, int count, CancellationToken ct = default)
    {
        var entityIds = await _db.DataMartNodes
            .AsNoTracking()
            .Where(n => n.ParentId == catalogNodeId && n.EntityId != null)
            .Select(n => n.EntityId!.Value)
            .Distinct()
            .ToListAsync(ct)
            .ConfigureAwait(false);
        if (entityIds.Count == 0)
            return (new List<DbEntityInfo>(), 0);
        var totalCount = await _db.DbEntities
            .AsNoTracking()
            .Where(e => entityIds.Contains(e.Id))
            .CountAsync(ct)
            .ConfigureAwait(false);
        var items = await _db.DbEntities
            .AsNoTracking()
            .Where(e => entityIds.Contains(e.Id))
            .OrderBy(e => e.Name)
            .Skip(offset)
            .Take(Math.Max(1, count))
            .Select(e => new DbEntityInfo
            {
                Id = e.Id,
                Name = e.Name,
                DisplayName = e.DisplayName,
                Description = e.Description,
            })
            .ToListAsync(ct)
            .ConfigureAwait(false);
        return (items, totalCount);
    }

    public async Task<DbEntityInfo?> GetEntityByIdAsync(Guid entityId, CancellationToken ct = default)
    {
        var e = await _db.DbEntities
            .AsNoTracking()
            .Where(x => x.Id == entityId)
            .Select(x => new { x.Id, x.Name, x.DisplayName, x.Description })
            .FirstOrDefaultAsync(ct)
            .ConfigureAwait(false);
        if (e == null) return null;
        return new DbEntityInfo { Id = e.Id, Name = e.Name, DisplayName = e.DisplayName, Description = e.Description };
    }

    public async Task<DbEntityInfo> CreateEntityAsync(CreateDbEntityParams create, CancellationToken ct = default)
    {
        var entity = new DbEntityDescription
        {
            Id = Guid.NewGuid(),
            Name = create.Name.Trim(),
            DisplayName = create.DisplayName?.Trim(),
            Description = create.Description?.Trim(),
        };
        _db.DbEntities.Add(entity);
        await _db.SaveChangesAsync(ct).ConfigureAwait(false);
        return new DbEntityInfo
        {
            Id = entity.Id,
            Name = entity.Name,
            DisplayName = entity.DisplayName,
            Description = entity.Description,
        };
    }

    public async Task<DbEntityInfo?> UpdateEntityAsync(Guid entityId, UpdateDbEntityParams update, CancellationToken ct = default)
    {
        var entity = await _db.DbEntities.FirstOrDefaultAsync(x => x.Id == entityId, ct).ConfigureAwait(false);
        if (entity == null) return null;
        if (update.Name != null) entity.Name = update.Name.Trim();
        if (update.DisplayName != null) entity.DisplayName = update.DisplayName.Trim();
        if (update.Description != null) entity.Description = update.Description.Trim();
        await _db.SaveChangesAsync(ct).ConfigureAwait(false);
        return await GetEntityByIdAsync(entityId, ct).ConfigureAwait(false);
    }

    public async Task<bool> DeleteEntityAsync(Guid entityId, CancellationToken ct = default)
    {
        var entity = await _db.DbEntities.FirstOrDefaultAsync(x => x.Id == entityId, ct).ConfigureAwait(false);
        if (entity == null) return false;
        _db.DbEntities.Remove(entity);
        await _db.SaveChangesAsync(ct).ConfigureAwait(false);
        return true;
    }

    public async Task<IReadOnlyList<DbFieldInfo>> GetFieldsAsync(Guid entityId, CancellationToken ct = default)
    {
        return await _db.DbFields
            .AsNoTracking()
            .Where(f => f.EntityId == entityId)
            .OrderBy(f => f.Name)
            .Select(f => new DbFieldInfo
            {
                Id = f.Id,
                EntityId = f.EntityId,
                Name = f.Name,
                DataType = f.DataType,
                IsNullable = f.IsNullable,
                IsPrimaryKey = f.IsPrimaryKey,
                Description = f.Description,
            })
            .ToListAsync(ct)
            .ConfigureAwait(false);
    }

    public async Task<DbFieldInfo> CreateFieldAsync(CreateDbFieldParams create, CancellationToken ct = default)
    {
        var field = new DbFieldDescription
        {
            Id = Guid.NewGuid(),
            EntityId = create.EntityId,
            Name = create.Name.Trim(),
            DataType = create.DataType?.Trim(),
            IsNullable = create.IsNullable,
            IsPrimaryKey = create.IsPrimaryKey,
            Description = create.Description?.Trim(),
        };
        _db.DbFields.Add(field);
        await _db.SaveChangesAsync(ct).ConfigureAwait(false);
        return new DbFieldInfo
        {
            Id = field.Id,
            EntityId = field.EntityId,
            Name = field.Name,
            DataType = field.DataType,
            IsNullable = field.IsNullable,
            IsPrimaryKey = field.IsPrimaryKey,
            Description = field.Description,
        };
    }

    public async Task<DbFieldInfo?> UpdateFieldAsync(Guid fieldId, UpdateDbFieldParams update, CancellationToken ct = default)
    {
        var field = await _db.DbFields.FirstOrDefaultAsync(x => x.Id == fieldId, ct).ConfigureAwait(false);
        if (field == null) return null;
        if (update.Name != null) field.Name = update.Name.Trim();
        if (update.DataType != null) field.DataType = update.DataType.Trim();
        if (update.IsNullable.HasValue) field.IsNullable = update.IsNullable.Value;
        if (update.IsPrimaryKey.HasValue) field.IsPrimaryKey = update.IsPrimaryKey.Value;
        if (update.Description != null) field.Description = update.Description.Trim();
        await _db.SaveChangesAsync(ct).ConfigureAwait(false);
        return new DbFieldInfo
        {
            Id = field.Id,
            EntityId = field.EntityId,
            Name = field.Name,
            DataType = field.DataType,
            IsNullable = field.IsNullable,
            IsPrimaryKey = field.IsPrimaryKey,
            Description = field.Description,
        };
    }

    public async Task<bool> DeleteFieldAsync(Guid fieldId, CancellationToken ct = default)
    {
        var field = await _db.DbFields.FirstOrDefaultAsync(x => x.Id == fieldId, ct).ConfigureAwait(false);
        if (field == null) return false;
        _db.DbFields.Remove(field);
        await _db.SaveChangesAsync(ct).ConfigureAwait(false);
        return true;
    }
}
