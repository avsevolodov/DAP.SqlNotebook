//using DAP.SqlNotebook.UI;
//using Microsoft.AspNetCore.Components.Web;
//using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
//using Microsoft.Extensions.DependencyInjection;
//using MudBlazor.Services;
//using System;

//var builder = WebAssemblyHostBuilder.CreateDefault(args);
//builder.RootComponents.Add<App>("#app");
//builder.RootComponents.Add<HeadOutlet>("head::after");

//builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });
//builder.Services.AddMudServices();



//builder.Services.AddSingleton(sp => new SqlNotebook.BL.YdbRulesRepository("Host=localhost;Port=2136;Database=/local"));
//builder.Services.AddSingleton<RulesService>();


//await builder.Build().RunAsync();

using DAP.SqlNotebook.Service.Client.V1;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor;

namespace DAP.SqlNotebook.UI;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddClients(this IServiceCollection services)
    {
        return services
            .AddScoped<INotebookManager, NotebookManager>()
            .AddScoped<IWorkspaceManager, WorkspaceManager>();
    }
}


internal static class SnackbarExtensions
{
    public static void ShowPopupMessage(this ISnackbar snackbar, string message, Severity severity = Severity.Info)
    {
#pragma warning disable IDISP004 // Don't ignore created IDisposable
        snackbar.Add(message, severity);
#pragma warning restore IDISP004 // Don't ignore created IDisposable
    }

    public static void ShowPopupMessage(this ISnackbar snackbar, MarkupString message, Severity severity = Severity.Info)
    {
#pragma warning disable IDISP004 // Don't ignore created IDisposable
        snackbar.Add(message, severity);
#pragma warning restore IDISP004 // Don't ignore created IDisposable
    }
}