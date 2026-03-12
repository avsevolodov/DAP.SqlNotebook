using DAP.SqlNotebook.Service.Client.V1;
using Microsoft.Extensions.DependencyInjection;

namespace DAP.SqlNotebook.UI;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddClients(this IServiceCollection services)
    {
        return services
            .AddScoped<INotebookManager, NotebookManager>()
            .AddScoped<IWorkspaceManager, WorkspaceManager>()
            .AddScoped<INotebookFavoritesClient, NotebookFavoritesClient>()
            .AddScoped<ICatalogManager, CatalogManager>()
            .AddScoped<IAiAssistClient, AiAssistClient>()
            .AddScoped<IAiSqlClient, AiSqlClient>()
            .AddScoped<ISchemaClient, SchemaClient>();
    }
}