using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Confluent.Kafka;
using DAP.SqlNotebook.BL.DataAccess;
using DAP.SqlNotebook.BL.DataAccess.Entities;
using DAP.SqlNotebook.BL.Models;
using DAP.SqlNotebook.BL.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace DAP.SqlNotebook.Service.Services.Kafka;

public sealed class KafkaCatalogService : IKafkaCatalogService, IKafkaMessageReader
{
    private const string DefaultConsumerGroupPrefix = "sqlnotebook-peek";

    private readonly ICatalogRepository _catalog;
    private readonly IDataSourcePasswordProtector _passwordProtector;
    private readonly ILogger<KafkaCatalogService> _logger;
    private readonly string _defaultConsumerGroupPrefix;

    public KafkaCatalogService(
        ICatalogRepository catalog,
        IDataSourcePasswordProtector passwordProtector,
        ILogger<KafkaCatalogService> logger,
        IConfiguration configuration)
    {
        _catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
        _passwordProtector = passwordProtector ?? throw new ArgumentNullException(nameof(passwordProtector));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        var kafkaSection = configuration?.GetSection("SqlNotebook")?.GetSection("Kafka");
        // Backward compatible: old setting name was ConsumerGroupId; now treated as prefix default.
        _defaultConsumerGroupPrefix = kafkaSection?.GetValue("ConsumerGroupId", DefaultConsumerGroupPrefix) ?? DefaultConsumerGroupPrefix;
    }

    public async Task EnsureTopicsLoadedAsync(Guid kafkaNodeId, CancellationToken ct = default)
    {
        var node = await _catalog.GetNodeByIdAsync(kafkaNodeId, ct).ConfigureAwait(false);
        if (node == null || !string.Equals(node.Provider, "Kafka", StringComparison.OrdinalIgnoreCase))
            return;

        var config = BuildAdminConfig(node);
        if (config == null) return;

        try
        {
            using var admin = new AdminClientBuilder(config).Build();
            var meta = admin.GetMetadata(TimeSpan.FromSeconds(15));
            var existingChildren = await _catalog.GetNodesAsync(kafkaNodeId, ct).ConfigureAwait(false);
            var existingNames = existingChildren.Select(c => c.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);

            foreach (var topic in meta.Topics.Where(t => !t.Error.IsError && !t.Topic.StartsWith("__")))
            {
                if (existingNames.Contains(topic.Topic)) continue;
                await _catalog.CreateNodeAsync(new CreateCatalogNodeParams
                {
                    ParentId = kafkaNodeId,
                    Type = (int)DataMartNodeType.Topic,
                    Name = topic.Topic,
                    Provider = "Kafka",
                }, ct).ConfigureAwait(false);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Kafka EnsureTopicsLoaded for node {NodeId}", kafkaNodeId);
        }
    }

    public async Task<ConnectionHealthResult> CheckAsync(Guid nodeId, CancellationToken ct = default)
    {
        var node = await _catalog.GetNodeByIdAsync(nodeId, ct).ConfigureAwait(false);
        if (node == null)
            return new ConnectionHealthResult { Status = 0, Message = "Node not found" };
        if (!string.Equals(node.Provider, "Kafka", StringComparison.OrdinalIgnoreCase))
            return new ConnectionHealthResult { Status = 0, Message = "Not a Kafka source" };
        if (string.IsNullOrWhiteSpace(node.ConnectionInfo))
            return new ConnectionHealthResult { Status = 0, Message = "No bootstrap servers configured" };

        var config = BuildAdminConfig(node);
        if (config == null)
            return new ConnectionHealthResult { Status = 0, Message = "Unsupported auth or invalid config" };

        try
        {
            using var admin = new AdminClientBuilder(config).Build();
            _ = admin.GetMetadata(TimeSpan.FromSeconds(10));
            return new ConnectionHealthResult { Status = 1, Message = "OK" };
        }
        catch (Exception ex)
        {
            return new ConnectionHealthResult { Status = 2, Message = ex.Message };
        }
    }

    public async Task<QueryResult> GetLatestMessagesAsync(Guid kafkaNodeId, string topicName, int limit, int timeoutSec, CancellationToken ct = default)
    {
        var node = await _catalog.GetNodeByIdAsync(kafkaNodeId, ct).ConfigureAwait(false);
        if (node == null || string.IsNullOrWhiteSpace(topicName))
            return new QueryResult { ColumnNames = new[] { "Error" }, Rows = new[] { new[] { "Source not found or topic empty." } as IReadOnlyList<string> } };

        var adminConfig = BuildAdminConfig(node);
        var consumerConfig = BuildConsumerConfig(node);
        if (adminConfig == null || consumerConfig == null)
            return new QueryResult { ColumnNames = new[] { "Error" }, Rows = new[] { new[] { "Invalid Kafka config." } as IReadOnlyList<string> } };

        var columnNames = new[] { "Partition", "Offset", "Key", "Value", "Timestamp" };
        var rows = new List<IReadOnlyList<string>>();

        try
        {
            List<TopicPartition> partitions;
            using (var admin = new AdminClientBuilder(adminConfig).Build())
            {
                var meta = admin.GetMetadata(TimeSpan.FromSeconds(timeoutSec));
                var topicMeta = meta.Topics.FirstOrDefault(t => t.Topic == topicName && !t.Error.IsError);
                if (topicMeta == null || topicMeta.Partitions.Count == 0)
                    return new QueryResult { ColumnNames = columnNames, Rows = rows };
                partitions = topicMeta.Partitions.Select(p => new TopicPartition(topicName, p.PartitionId)).ToList();
            }

            var perPartition = Math.Max(1, (limit + partitions.Count - 1) / partitions.Count);
            using var consumer = new ConsumerBuilder<Ignore, string>(consumerConfig).Build();
            consumer.Assign(partitions.Select(p => new TopicPartitionOffset(p, Offset.Beginning)));
            foreach (var tp in partitions)
            {
                var wo = consumer.QueryWatermarkOffsets(tp, TimeSpan.FromSeconds(timeoutSec));
                var seekOffset = Math.Max(0, wo.High.Value - perPartition);
                consumer.Seek(new TopicPartitionOffset(tp, seekOffset));
            }

            var timeout = TimeSpan.FromSeconds(timeoutSec);
            var remaining = limit;
            while (remaining > 0 && !ct.IsCancellationRequested)
            {
                var cr = consumer.Consume(timeout);
                if (cr == null) break;
                // Key is Ignore when using ConsumerBuilder<Ignore, string>; show empty
                var key = "";
                var value = cr.Message.Value ?? "";
                var ts = cr.Message.Timestamp.Type != TimestampType.NotAvailable
                    ? cr.Message.Timestamp.UtcDateTime.ToString("O")
                    : "";
                rows.Add(new[]
                {
                    cr.Partition.Value.ToString(),
                    cr.Offset.Value.ToString(),
                    key,
                    value.Length > 2000 ? value.Substring(0, 2000) + "…" : value,
                    ts,
                });
                remaining--;
            }

            return new QueryResult { ColumnNames = columnNames, Rows = rows };
        }
        catch (Exception ex)
        {
            return new QueryResult
            {
                ColumnNames = columnNames,
                Rows = new List<IReadOnlyList<string>> { new[] { "", "", "", ex.Message, "" } },
            };
        }
    }

    private AdminClientConfig? BuildAdminConfig(CatalogNode node)
    {
        var bootstrap = node.ConnectionInfo?.Trim();
        if (string.IsNullOrEmpty(bootstrap)) return null;

        var isKerberos = !string.Equals(node.AuthType?.Trim(), "Basic", StringComparison.OrdinalIgnoreCase);
        var config = new AdminClientConfig
        {
            BootstrapServers = bootstrap,
        };
        if (isKerberos)
        {
            config.SecurityProtocol = SecurityProtocol.SaslSsl;
            config.SaslMechanism = SaslMechanism.Gssapi;
            config.ApiVersionRequest = true;
            config.SaslKerberosServiceName = "kafka";
            config.SslCaCertificateStores = "Root,CA";
        }
        else
        {
            config.SecurityProtocol = SecurityProtocol.Plaintext;
        }

        return config;
    }

    private ConsumerConfig? BuildConsumerConfig(CatalogNode node)
    {
        var bootstrap = node.ConnectionInfo?.Trim();
        if (string.IsNullOrEmpty(bootstrap)) return null;

        var isKerberos = !string.Equals(node.AuthType?.Trim(), "Basic", StringComparison.OrdinalIgnoreCase);
        var config = new ConsumerConfig
        {
            BootstrapServers = bootstrap,
            GroupId = BuildConsumerGroupId(node),
            EnableAutoCommit = false,
            //AutoOffsetReset = AutoOffsetReset.Latest,
            //EnableAutoOffsetStore = false,
        };
        if (isKerberos)
        {
            config.SecurityProtocol = SecurityProtocol.SaslSsl;
            config.SaslMechanism = SaslMechanism.Gssapi;
            config.SaslKerberosServiceName = "kafka";
            config.SslCaCertificateStores = "Root,CA";
        }
        else
        {
            config.SecurityProtocol = SecurityProtocol.Plaintext;
        }

        return config;
    }

    private string BuildConsumerGroupId(CatalogNode node)
    {
        var prefix = (node.ConsumerGroupPrefix ?? _defaultConsumerGroupPrefix ?? DefaultConsumerGroupPrefix).Trim();
        if (string.IsNullOrWhiteSpace(prefix))
            prefix = DefaultConsumerGroupPrefix;
        if (!node.ConsumerGroupAutoGenerate)
            return prefix;
        return $"{prefix}-{Guid.NewGuid():N}";
    }
}
