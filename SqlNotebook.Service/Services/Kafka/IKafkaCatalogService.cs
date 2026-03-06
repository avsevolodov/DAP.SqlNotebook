using System;
using System.Threading;
using System.Threading.Tasks;
using DAP.SqlNotebook.BL.Services;

namespace DAP.SqlNotebook.Service.Services.Kafka;

/// <summary>
/// Kafka-specific catalog operations: load topics on demand, health check.
/// </summary>
public interface IKafkaCatalogService
{
    /// <summary>Ensure child topic nodes exist for a Kafka source node; loads from broker if needed.</summary>
    Task EnsureTopicsLoadedAsync(Guid kafkaNodeId, CancellationToken ct = default);

    /// <summary>Check connectivity to the Kafka cluster (node must be a Kafka Database source).</summary>
    Task<ConnectionHealthResult> CheckAsync(Guid nodeId, CancellationToken ct = default);
}
