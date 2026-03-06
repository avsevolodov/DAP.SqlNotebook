using System;

namespace DAP.SqlNotebook.UI.Helpers
{
    public static class MainRoutes
    {
        public const string Rules = "/rules";
        public const string Notebooks = "/notebooks";
        public const string SchemaCatalog = "/schema-catalog";
        public const string Workspaces = "/workspaces";

        public static string GetRulesRoute(int? page = null, int? pageSize = null)
        {
            var queryParameters = RoutesHelper.GetPagingQueryPamateres(page, pageSize);
            return string.Concat(Rules, queryParameters ?? "");
        }

        public static string GetNotebookRoute(Guid id) => $"{Notebooks}/{id}";
        public static string GetNewNotebookRoute(Guid? workspaceId = null)
            => workspaceId.HasValue ? $"{Notebooks}/new?workspaceId={workspaceId.Value:N}" : $"{Notebooks}/new";
        public static string GetWorkspaceRoute(Guid id) => $"{Workspaces}/{id}";
    }
}
