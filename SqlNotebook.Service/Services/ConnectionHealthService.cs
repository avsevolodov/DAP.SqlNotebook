using System;
using System.Threading;
using System.Threading.Tasks;
using DAP.SqlNotebook.BL.DataAccess;
using DAP.SqlNotebook.BL.Services;
using DAP.SqlNotebook.Service.Services.Database;
using DAP.SqlNotebook.Service.Services.Kafka;
using Microsoft.EntityFrameworkCore;

namespace DAP.SqlNotebook.Service.Services;

public sealed class ConnectionHealthService : IConnectionHealthService
{
    private readonly SqlNotebookDbContext _db;
    private readonly ICatalogRepository _catalog;
    private readonly IDbProviderStrategyFactory _strategyFactory;
    private readonly IDataSourcePasswordProtector _passwordProtector;
    private readonly IKafkaCatalogService _kafkaCatalog;

    public ConnectionHealthService(SqlNotebookDbContext db, ICatalogRepository catalog, IDbProviderStrategyFactory strategyFactory, IDataSourcePasswordProtector passwordProtector, IKafkaCatalogService kafkaCatalog)
    {
        _db = db ?? throw new ArgumentNullException(nameof(db));
        _catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
        _strategyFactory = strategyFactory ?? throw new ArgumentNullException(nameof(strategyFactory));
        _passwordProtector = passwordProtector ?? throw new ArgumentNullException(nameof(passwordProtector));
        _kafkaCatalog = kafkaCatalog ?? throw new ArgumentNullException(nameof(kafkaCatalog));
    }

    public async Task<bool> CheckAsync(CancellationToken ct = default)
    {
        try
        {
            _ = await _db.Database.CanConnectAsync(ct).ConfigureAwait(false);
            return true;
        }
        catch
        {
            return false;
        }
    }

    public async Task<ConnectionHealthResult> CheckNodeAsync(Guid nodeId, CancellationToken ct = default)
    {
        try
        {
            var node = await _catalog.GetNodeByIdAsync(nodeId, ct).ConfigureAwait(false);
            if (node == null)
                return new ConnectionHealthResult { Status = 0, Message = "Node not found" };
            if (string.IsNullOrWhiteSpace(node.ConnectionInfo))
                return new ConnectionHealthResult { Status = 0, Message = "No connection configured" };

            if (string.Equals(node.Provider, "Kafka", StringComparison.OrdinalIgnoreCase))
                return await _kafkaCatalog.CheckAsync(nodeId, ct).ConfigureAwait(false);

            var strategy = _strategyFactory.GetStrategy(node.Provider);
            if (strategy == null)
                return new ConnectionHealthResult { Status = 0, Message = "Provider not supported: " + (node.Provider ?? "unknown") };

            string? password = null;
            var useBasicAuth = string.Equals(node.AuthType?.Trim(), "Basic", StringComparison.OrdinalIgnoreCase);
            if (useBasicAuth && !string.IsNullOrEmpty(node.PasswordEncrypted))
            {
                try { password = _passwordProtector.Unprotect(node.PasswordEncrypted); }
                catch { return new ConnectionHealthResult { Status = 2, Message = "Failed to decrypt stored password" }; }
            }
            else if (!string.IsNullOrWhiteSpace(node.Login) && !string.IsNullOrEmpty(node.PasswordEncrypted))
            {
                useBasicAuth = true;
                try { password = _passwordProtector.Unprotect(node.PasswordEncrypted); }
                catch { return new ConnectionHealthResult { Status = 2, Message = "Failed to decrypt stored password" }; }
            }
            var authTypeForBuild = useBasicAuth ? "Basic" : node.AuthType;
            var connStr = strategy.BuildConnectionString(node.ConnectionInfo.Trim(), node.DatabaseName, authTypeForBuild, node.Login, password);
            return await strategy.CheckAsync(connStr, ct).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            return new ConnectionHealthResult { Status = 2, Message = ex.Message };
        }
    }
}
