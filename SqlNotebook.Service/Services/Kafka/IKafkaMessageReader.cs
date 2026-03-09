using System;
using System.Threading;
using System.Threading.Tasks;
using DAP.SqlNotebook.BL.Services;

namespace DAP.SqlNotebook.Service.Services.Kafka;

/// <summary>
/// Low-level Kafka message reader: read latest messages from a topic.
/// </summary>
public interface IKafkaMessageReader
{
    /// <summary>
    /// Read up to <paramref name="limit"/> latest messages from the topic.
    /// <paramref name="kafkaNodeId"/> is the Kafka source/database node (parent of topic in catalog).
    /// </summary>
    Task<QueryResult> GetLatestMessagesAsync(Guid kafkaNodeId, string topicName, int limit, int timeoutSec, CancellationToken ct = default);
}

