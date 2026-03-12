using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using DAP.SqlNotebook.BL.DataAccess;
using DAP.SqlNotebook.BL.Models;

namespace DAP.SqlNotebook.BL.Services.AiSql.Autocomplete;

/// <summary>
/// Builds and caches SchemaGraph from catalog repository for fast autocomplete.
/// </summary>
public interface ISchemaGraphFactory
{
    /// <summary>
    /// Get in-memory schema graph for all entities (or by source node when supported).
    /// Cached in memory; can be invalidated via ReloadAsync.
    /// </summary>
    Task<SchemaGraph> GetAsync(CancellationToken ct = default);

    /// <summary>
    /// Force reload of schema graph from catalog repository.
    /// </summary>
    Task<SchemaGraph> ReloadAsync(CancellationToken ct = default);
}

public sealed class SchemaGraphFactory : ISchemaGraphFactory
{
    private readonly ICatalogRepository _catalog;
    private readonly SemaphoreSlim _buildLock = new(1, 1);
    private volatile SchemaGraph? _cached;

    public SchemaGraphFactory(ICatalogRepository catalog)
    {
        _catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
    }

    public Task<SchemaGraph> GetAsync(CancellationToken ct = default)
    {
        var cached = _cached;
        if (cached is not null)
            return Task.FromResult(cached);
        return BuildInternalAsync(forceReload: false, ct);
    }

    public Task<SchemaGraph> ReloadAsync(CancellationToken ct = default)
    {
        return BuildInternalAsync(forceReload: true, ct);
    }

    private async Task<SchemaGraph> BuildInternalAsync(bool forceReload, CancellationToken ct)
    {
        if (!forceReload && _cached is not null)
            return _cached;

        await _buildLock.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            if (!forceReload && _cached is not null)
                return _cached;

            var entities = await _catalog.GetEntitiesAsync(ct).ConfigureAwait(false);
            var tables = new List<SchemaTable>(entities.Count);

            foreach (var e in entities)
            {
                var fields = await _catalog.GetFieldsAsync(e.Id, ct).ConfigureAwait(false);
                var columns = new List<SchemaColumn>(fields.Count);
                foreach (DbFieldInfo f in fields)
                {
                    columns.Add(new SchemaColumn(
                        name: f.Name ?? string.Empty,
                        dataType: f.DataType,
                        description: f.Description));
                }

                tables.Add(new SchemaTable(
                    name: e.Name ?? string.Empty,
                    description: e.Description,
                    columns: columns));
            }

            // Relations are not yet modelled in catalog; keep empty list for now.
            var graph = new SchemaGraph(tables, Array.Empty<SchemaRelation>());
            _cached = graph;
            return graph;
        }
        finally
        {
            _buildLock.Release();
        }
    }
}

