using System;
using System.Net.Http;
using System.Threading.Tasks;
using Blazor.Extensions.Logging;
using DAP.Markdown;
using DAP.SqlNotebook.UI.Helpers;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MudBlazor;
using MudBlazor.Services;

namespace DAP.SqlNotebook.UI
{
    static class Program
    {
        static async Task Main(string[] args)
        {
            var builder = WebAssemblyHostBuilder.CreateDefault(args);

            builder.Services
                .AddScoped(sp => sp.GetRequiredService<IHttpClientFactory>().CreateClient("api"))
                .AddClients()
                .AddHttpClient("api", options =>
                {
                    options.BaseAddress = new Uri(builder.HostEnvironment.BaseAddress);
                });

            builder.Services
                .AddAuthorizationCore()
                .AddScoped<ClientAuthenticationStateProvider>()
                .AddScoped<AuthenticationStateProvider>(
                    p => p.GetRequiredService<ClientAuthenticationStateProvider>())
                .AddScoped<ICurrentUserService, CurrentUserService>();

            builder.Services.AddScoped<ICatalogInsertService, CatalogInsertService>();
            builder.Services.AddSingleton<ICatalogRefreshService, CatalogRefreshService>();
            builder.Services.AddScoped<IAiAssistInsertService, AiAssistInsertService>();
            builder.Services.AddScoped<ICurrentSqlContext, CurrentSqlContext>();
            builder.Services.AddScoped<ICurrentNotebookContext, CurrentNotebookContext>();
            builder.Services.AddScoped<ICurrentDatabaseContext, CurrentDatabaseContext>();
            builder.Services.AddScoped<INotebookToolbarService, NotebookToolbarService>();

            // builder.Services.AddScoped<IConnectionManager, ConnectionManager>();

            builder.Services
                .AddMudServices(config =>
                {
                    config.SnackbarConfiguration.PositionClass = Defaults.Classes.Position.BottomCenter;
                })
                .AddMudMarkdownServices();

            // builder.Services.AddTransient<QueryService>();
            // builder.Services.AddTransient<global::SqlNotebook.Services.ConnectionManager>();


            builder.Logging.AddBrowserConsole().SetMinimumLevel(LogLevel.Debug); // Logs to browser console


            await builder.Build().RunAsync();
        }
    }
}
