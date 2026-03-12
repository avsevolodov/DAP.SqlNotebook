namespace DAP.SqlNotebook.Contract;

/// <summary>
/// Centralized API route base paths and templates. Used by Service controllers and Client EndpointsHelper.
/// </summary>
public static class ApiRoutes
{
    public const string Notebooks = "api/v1/notebooks";
    public const string Workspaces = "api/v1/workspaces";
    public const string Catalog = "api/v1/catalog";
    public const string Favorites = "api/v1/favorites";
    public const string AiAssist = "api/v1/ai/assist";
    public const string AiSql = "api/v1/ai/sql";
    public const string Schema = "api/v1/schema";

    /// <summary>Route template for Excalidraw controller: api/v1/notebooks/{notebookId:guid}/excalidraw</summary>
    public const string Excalidraw = Notebooks + "/{notebookId:guid}/excalidraw";
}
