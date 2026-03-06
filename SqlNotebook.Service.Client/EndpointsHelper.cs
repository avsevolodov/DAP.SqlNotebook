using System.Text;

namespace DAP.SqlNotebook.Service.Client
{
    public static class EndpointsHelper
    {
        public const string Notebooks = "api/v1/notebooks";
        public const string Workspaces = "api/v1/workspaces";

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
