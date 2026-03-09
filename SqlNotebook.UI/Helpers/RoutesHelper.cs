namespace DAP.SqlNotebook.UI.Helpers
{
    public static class RoutesHelper
    {
        public static string? GetPagingQueryPamateres(int? page = null, int? pageSize = null)
        {
            string? queryParameters = null;

            if (page.HasValue)
            {
                queryParameters = $"?{nameof(page)}={page}";
            }

            if (pageSize.HasValue)
            {
                var separator = string.IsNullOrEmpty(queryParameters) ? '?' : '&';
                queryParameters = string.Concat(queryParameters, separator, $"{nameof(pageSize)}={pageSize}");
            }

            return queryParameters;
        }
    }


}
