using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DAP.SqlNotebook.BL.DataAccess;
using DAP.SqlNotebook.BL.Models;
using DAP.SqlNotebook.BL.Services;
using DAP.SqlNotebook.Service.Services.Database;
using DAP.SqlNotebook.Service.Services.Kafka;

namespace DAP.SqlNotebook.Service.Services;

public sealed class SchemaImportService : ISchemaImportService
{
    private readonly ICatalogRepository _catalog;
    private readonly IDbProviderStrategyFactory _strategyFactory;
    private readonly IDataSourcePasswordProtector _passwordProtector;
    private readonly IKafkaCatalogService _kafkaCatalog;

    public SchemaImportService(ICatalogRepository catalog, IDbProviderStrategyFactory strategyFactory, IDataSourcePasswordProtector passwordProtector, IKafkaCatalogService kafkaCatalog)
    {
        _catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
        _strategyFactory = strategyFactory ?? throw new ArgumentNullException(nameof(strategyFactory));
        _passwordProtector = passwordProtector ?? throw new ArgumentNullException(nameof(passwordProtector));
        _kafkaCatalog = kafkaCatalog ?? throw new ArgumentNullException(nameof(kafkaCatalog));
    }

    public async Task<SchemaImportResult> ImportStructureAsync(Guid nodeId, CancellationToken ct = default)
    {
        try
        {
            var node = await _catalog.GetNodeByIdAsync(nodeId, ct).ConfigureAwait(false);
            if (node == null)
                return new SchemaImportResult { Error = "Node not found" };
            if (string.IsNullOrWhiteSpace(node.ConnectionInfo))
                return new SchemaImportResult { Error = "No connection configured" };

            if (string.Equals(node.Provider, "Kafka", StringComparison.OrdinalIgnoreCase))
            {
                await _kafkaCatalog.EnsureTopicsLoadedAsync(nodeId, ct).ConfigureAwait(false);
                var children = await _catalog.GetNodesAsync(nodeId, ct).ConfigureAwait(false);
                return new SchemaImportResult { TablesCount = children.Count, FieldsCount = 0 };
            }

            var strategy = _strategyFactory.GetStrategy(node.Provider);
            if (strategy == null)
                return new SchemaImportResult { Error = "Import not implemented for " + (node.Provider ?? "unknown") };

            string? password = null;
            var useBasicAuth = string.Equals(node.AuthType?.Trim(), "Basic", StringComparison.OrdinalIgnoreCase);
            if (useBasicAuth && !string.IsNullOrEmpty(node.PasswordEncrypted))
            {
                try { password = _passwordProtector.Unprotect(node.PasswordEncrypted); }
                catch (Exception ex) { return new SchemaImportResult { Error = "Failed to decrypt stored password: " + ex.Message }; }
            }
            else if (!string.IsNullOrWhiteSpace(node.Login) && !string.IsNullOrEmpty(node.PasswordEncrypted))
            {
                useBasicAuth = true;
                try { password = _passwordProtector.Unprotect(node.PasswordEncrypted); }
                catch (Exception ex) { return new SchemaImportResult { Error = "Failed to decrypt stored password: " + ex.Message }; }
            }
            var authTypeForBuild = useBasicAuth ? "Basic" : node.AuthType;
            var connStr = strategy.BuildConnectionString(node.ConnectionInfo.Trim(), node.DatabaseName, authTypeForBuild, node.Login, password);
            var (tables, columns) = await strategy.ReadMetadataAsync(connStr, ct).ConfigureAwait(false);

            var existingChildren = await _catalog.GetNodesAsync(nodeId, ct).ConfigureAwait(false);
            var existingByKey = new Dictionary<string, (CatalogNode Node, DbEntityInfo Entity, List<DbFieldInfo> Fields)>(StringComparer.OrdinalIgnoreCase);
            foreach (var child in existingChildren.Where(c => c.EntityId.HasValue))
            {
                var entity = await _catalog.GetEntityByIdAsync(child.EntityId!.Value, ct).ConfigureAwait(false);
                if (entity == null) continue;
                var key = NormalizeQualifiedName(entity.Name);
                var fields = (await _catalog.GetFieldsAsync(entity.Id, ct).ConfigureAwait(false)).ToList();
                existingByKey[key] = (child, entity, fields);
            }

            var tablesCount = 0;
            var fieldsCount = 0;

            foreach (var t in tables.OrderBy(x => x.Schema).ThenBy(x => x.Name))
            {
                var qualifiedName = t.QualifiedName;
                var key = NormalizeQualifiedName(qualifiedName);
                if (existingByKey.TryGetValue(key, out var existing))
                {
                    existingByKey.Remove(key);
                    var (childNode, entity, existingFields) = existing;
                    await _catalog.UpdateEntityAsync(entity.Id, new UpdateDbEntityParams
                    {
                        Name = qualifiedName,
                        DisplayName = t.Name,
                        Description = entity.Description,
                    }, ct).ConfigureAwait(false);

                    var tableCols = columns
                        .Where(c => string.Equals(c.TableSchema, t.Schema, StringComparison.OrdinalIgnoreCase) &&
                                    string.Equals(c.TableName, t.Name, StringComparison.OrdinalIgnoreCase))
                        .OrderBy(c => c.OrdinalPosition)
                        .ToList();
                    var existingFieldsByName = existingFields.ToDictionary(f => f.Name, f => f, StringComparer.OrdinalIgnoreCase);

                    foreach (var c in tableCols)
                    {
                        if (existingFieldsByName.TryGetValue(c.ColumnName, out var existingField))
                        {
                            await _catalog.UpdateFieldAsync(existingField.Id, new UpdateDbFieldParams
                            {
                                Name = c.ColumnName,
                                DataType = c.DataType,
                                IsNullable = c.IsNullable,
                                IsPrimaryKey = c.IsPrimaryKey,
                                Description = existingField.Description,
                            }, ct).ConfigureAwait(false);
                            existingFieldsByName.Remove(c.ColumnName);
                        }
                        else
                        {
                            await _catalog.CreateFieldAsync(new CreateDbFieldParams
                            {
                                EntityId = entity.Id,
                                Name = c.ColumnName,
                                DataType = c.DataType,
                                IsNullable = c.IsNullable,
                                IsPrimaryKey = c.IsPrimaryKey,
                                Description = null,
                            }, ct).ConfigureAwait(false);
                        }
                    }

                    foreach (var obsoleteField in existingFieldsByName.Values)
                        await _catalog.DeleteFieldAsync(obsoleteField.Id, ct).ConfigureAwait(false);

                    tablesCount++;
                    fieldsCount += tableCols.Count;
                }
                else
                {
                    var entity = await _catalog.CreateEntityAsync(new CreateDbEntityParams
                    {
                        Name = qualifiedName,
                        DisplayName = t.Name,
                        Description = null,
                    }, ct).ConfigureAwait(false);

                    await _catalog.CreateNodeAsync(new CreateCatalogNodeParams
                    {
                        ParentId = nodeId,
                        Type = 2,
                        Name = t.Name,
                        EntityId = entity.Id,
                    }, ct).ConfigureAwait(false);

                    tablesCount++;
                    var tableCols = columns
                        .Where(c => string.Equals(c.TableSchema, t.Schema, StringComparison.OrdinalIgnoreCase) &&
                                    string.Equals(c.TableName, t.Name, StringComparison.OrdinalIgnoreCase))
                        .OrderBy(c => c.OrdinalPosition)
                        .ToList();

                    foreach (var c in tableCols)
                    {
                        await _catalog.CreateFieldAsync(new CreateDbFieldParams
                        {
                            EntityId = entity.Id,
                            Name = c.ColumnName,
                            DataType = c.DataType,
                            IsNullable = c.IsNullable,
                            IsPrimaryKey = c.IsPrimaryKey,
                            Description = null,
                        }, ct).ConfigureAwait(false);
                        fieldsCount++;
                    }
                }
            }

            foreach (var (childNode, entity, _) in existingByKey.Values)
            {
                await _catalog.DeleteNodeAsync(childNode.Id, ct).ConfigureAwait(false);
                await _catalog.DeleteEntityAsync(entity.Id, ct).ConfigureAwait(false);
            }

            return new SchemaImportResult { TablesCount = tablesCount, FieldsCount = fieldsCount };
        }
        catch (Exception ex)
        {
            return new SchemaImportResult { Error = ex.Message };
        }
    }

    private static string NormalizeQualifiedName(string? name)
    {
        if (string.IsNullOrEmpty(name)) return string.Empty;
        return name.Trim();
    }
}
