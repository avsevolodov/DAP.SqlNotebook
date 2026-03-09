using System;
using System.Threading;
using System.Threading.Tasks;

namespace DAP.SqlNotebook.BL.Services;

public interface IConnectionHealthService
{
    Task<bool> CheckAsync(CancellationToken ct = default);

    /// <summary>Check connectivity for a catalog node (source).</summary>
    Task<ConnectionHealthResult> CheckNodeAsync(Guid nodeId, CancellationToken ct = default);
}

public sealed class ConnectionHealthResult
{
    public int Status { get; set; }
    public string? Message { get; set; }
}
