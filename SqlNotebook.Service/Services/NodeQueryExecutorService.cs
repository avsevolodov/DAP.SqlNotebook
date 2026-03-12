using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DAP.SqlNotebook.BL.DataAccess;
using DAP.SqlNotebook.Contract.Entities;
using DAP.SqlNotebook.Service.Services.Database;
using DAP.SqlNotebook.Service.Services.Kafka;

namespace DAP.SqlNotebook.Service.Services;

public sealed class NodeQueryExecutorService : INodeQueryExecutorService
{
    private readonly ICatalogRepository _catalog;
    private readonly IDbProviderStrategyFactory _strategyFactory;
    private readonly IDataSourcePasswordProtector _passwordProtector;
    private readonly IKafkaMessageReader _kafkaReader;

    public NodeQueryExecutorService(
        ICatalogRepository catalog,
        IDbProviderStrategyFactory strategyFactory,
        IDataSourcePasswordProtector passwordProtector,
        IKafkaMessageReader kafkaReader)
    {
        _catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
        _strategyFactory = strategyFactory ?? throw new ArgumentNullException(nameof(strategyFactory));
        _passwordProtector = passwordProtector ?? throw new ArgumentNullException(nameof(passwordProtector));
        _kafkaReader = kafkaReader ?? throw new ArgumentNullException(nameof(kafkaReader));
    }

    public async Task<NotebookCellExecutionResultInfo> ExecuteAsync(Guid catalogNodeId, string query, int timeoutSeconds, int? maxRows = null, CancellationToken ct = default)
    {
        try
        {
            var node = await _catalog.GetNodeByIdAsync(catalogNodeId, ct).ConfigureAwait(false);
            if (node == null)
                return new NotebookCellExecutionResultInfo { Status = NotebookCellExecutionStatusInfo.Failed, Error = "Database/source not found." };

            if (string.Equals(node.Type, "Topic", StringComparison.OrdinalIgnoreCase) && node.ParentId.HasValue)
            {
                var limit = 100;
                var kafkaResult = await _kafkaReader.GetLatestMessagesAsync(node.ParentId.Value, node.Name, limit, timeoutSeconds, ct).ConfigureAwait(false);
                return new NotebookCellExecutionResultInfo
                {
                    Status = NotebookCellExecutionStatusInfo.Success,
                    Columns = kafkaResult.ColumnNames.Select(n => new ExecutionResultColumnInfo { Name = n }).ToArray(),
                    Rows = kafkaResult.Rows.Select(values => new ExecutionResultRowInfo { Values = values.ToList() }).ToArray(),
                };
            }

            if (string.Equals(node.Provider, "Kafka", StringComparison.OrdinalIgnoreCase) && string.Equals(node.Type, "Database", StringComparison.OrdinalIgnoreCase))
            {
                var topicName = query?.Trim() ?? "";
                if (string.IsNullOrEmpty(topicName))
                    return new NotebookCellExecutionResultInfo { Status = NotebookCellExecutionStatusInfo.Failed, Error = "Enter topic name in the editor." };
                var limitKafka = 100;
                var kafkaResultDb = await _kafkaReader.GetLatestMessagesAsync(catalogNodeId, topicName, limitKafka, timeoutSeconds, ct).ConfigureAwait(false);
                return new NotebookCellExecutionResultInfo
                {
                    Status = NotebookCellExecutionStatusInfo.Success,
                    Columns = kafkaResultDb.ColumnNames.Select(n => new ExecutionResultColumnInfo { Name = n }).ToArray(),
                    Rows = kafkaResultDb.Rows.Select(values => new ExecutionResultRowInfo { Values = values.ToList() }).ToArray(),
                };
            }

            if (string.IsNullOrWhiteSpace(node.ConnectionInfo))
                return new NotebookCellExecutionResultInfo { Status = NotebookCellExecutionStatusInfo.Failed, Error = "No connection configured for this database." };

            var strategy = _strategyFactory.GetStrategy(node.Provider);
            if (strategy == null)
                return new NotebookCellExecutionResultInfo { Status = NotebookCellExecutionStatusInfo.Failed, Error = "Provider not supported: " + (node.Provider ?? "unknown") };

            string? password = null;
            var useBasicAuth = string.Equals(node.AuthType?.Trim(), "Basic", StringComparison.OrdinalIgnoreCase);
            if (useBasicAuth && !string.IsNullOrEmpty(node.PasswordEncrypted))
            {
                try { password = _passwordProtector.Unprotect(node.PasswordEncrypted); }
                catch (Exception ex) { return new NotebookCellExecutionResultInfo { Status = NotebookCellExecutionStatusInfo.Failed, Error = "Failed to decrypt password: " + ex.Message }; }
            }
            else if (!string.IsNullOrWhiteSpace(node.Login) && !string.IsNullOrEmpty(node.PasswordEncrypted))
            {
                useBasicAuth = true;
                try { password = _passwordProtector.Unprotect(node.PasswordEncrypted); }
                catch (Exception ex) { return new NotebookCellExecutionResultInfo { Status = NotebookCellExecutionStatusInfo.Failed, Error = "Failed to decrypt password: " + ex.Message }; }
            }
            var authTypeForBuild = useBasicAuth ? "Basic" : node.AuthType;
            var connStr = strategy.BuildConnectionString(node.ConnectionInfo.Trim(), node.DatabaseName, authTypeForBuild, node.Login, password);
            var result = await strategy.ExecuteQueryAsync(connStr, query, timeoutSeconds, maxRows, ct).ConfigureAwait(false);

            return new NotebookCellExecutionResultInfo
            {
                Status = NotebookCellExecutionStatusInfo.Success,
                Columns = result.ColumnNames.Select(n => new ExecutionResultColumnInfo { Name = n }).ToArray(),
                Rows = result.Rows.Select(values => new ExecutionResultRowInfo { Values = values.ToList() }).ToArray(),
            };
        }
        catch (OperationCanceledException)
        {
            return new NotebookCellExecutionResultInfo
            {
                Status = NotebookCellExecutionStatusInfo.Failed,
                Error = "Query was cancelled.",
            };
        }
        catch (Exception ex)
        {
            return new NotebookCellExecutionResultInfo
            {
                Status = NotebookCellExecutionStatusInfo.Failed,
                Error = ex.Message,
            };
        }
    }
}
