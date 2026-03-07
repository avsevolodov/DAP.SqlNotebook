using System.Text;

namespace DAP.SqlNotebook.Service.Client
{
    public static class EndpointsHelper
    {
        public const string Notebooks = "api/v1/notebooks";
        public const string Workspaces = "api/v1/workspaces";
        public const string Catalog = "api/v1/catalog";
        public const string AiAssist = "api/v1/ai/assist";
        public const string AiSql = "api/v1/ai/sql";

        public static string AiAssistSessions(Guid? notebookId = null)
            => notebookId.HasValue ? $"{AiAssist}/sessions?notebookId={notebookId.Value:N}" : $"{AiAssist}/sessions";
        public static string AiAssistMessages(Guid? sessionId = null)
            => sessionId.HasValue ? $"{AiAssist}/messages?sessionId={sessionId.Value:N}" : $"{AiAssist}/messages";
        public static string AiAssistSend() => $"{AiAssist}/send";
        public static string AiSqlSuggestChart() => $"{AiSql}/suggest-chart";

        public static string CatalogNodes(Guid? parentId = null)
            => parentId.HasValue ? $"{Catalog}/nodes?parentId={parentId.Value:N}" : $"{Catalog}/nodes";
        public static string CatalogNodesCreate() => $"{Catalog}/nodes";
        public static string CatalogDatabases() => $"{Catalog}/databases";
        public static string CatalogNode(Guid id) => $"{Catalog}/nodes/{id:N}";
        public static string CatalogImportStructure(Guid nodeId) => $"{Catalog}/nodes/{nodeId:N}/import-structure";
        public static string CatalogConnectionStatus(Guid nodeId) => $"{Catalog}/nodes/{nodeId:N}/connection-status";
        public static string EntitySelectText(Guid entityId, int? top = null)
            => top.HasValue ? $"{Catalog}/entity/{entityId:N}/select-text?top={top.Value}" : $"{Catalog}/entity/{entityId:N}/select-text";
        public static string CatalogEntities(Guid? nodeId = null, int? offset = null, int? count = null)
        {
            var sb = new StringBuilder($"{Catalog}/entities");
            var sep = "?";
            if (nodeId.HasValue) { sb.Append(sep).Append("nodeId=").Append(nodeId.Value.ToString("N")); sep = "&"; }
            if (offset.HasValue) { sb.Append(sep).Append("offset=").Append(offset.Value); sep = "&"; }
            if (count.HasValue) { sb.Append(sep).Append("count=").Append(count.Value); }
            return sb.ToString();
        }
        public static string CatalogEntity(Guid entityId) => $"{Catalog}/entities/{entityId:N}";
        public static string CatalogEntityFields(Guid entityId) => $"{Catalog}/entities/{entityId:N}/fields";
        public static string CatalogField(Guid fieldId) => $"{Catalog}/fields/{fieldId:N}";

        public static string CreateGetNotebooksRoute(
           int offset = 0,
           int batchSize = 100,
           string? queryFilter = null,
           Guid? workspaceId = null)
        {
            var sb = new StringBuilder(Notebooks);
            sb.Append("?batchSize=").Append(batchSize)
              .Append("&offset=").Append(offset);

            if (queryFilter != null)
            {
                sb.Append("&queryFilter=").Append(Uri.EscapeDataString(queryFilter));
            }

            if (workspaceId.HasValue)
            {
                sb.Append("&workspaceId=").Append(workspaceId.Value.ToString("N"));
            }

            return sb.ToString();
        }

        public static string GetNotebookByIdRoute(Guid notebookId)
            => $"{Notebooks}/{notebookId}";

        public static string GetNotebookExecuteRoute(Guid notebookId)
            => $"{Notebooks}/{notebookId}/execute";

        public static string GetWorkspaceByIdRoute(Guid workspaceId)
            => $"{Workspaces}/{workspaceId}";
    }
}
