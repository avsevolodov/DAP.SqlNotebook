using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor;

namespace DAP.SqlNotebook.UI.Helpers;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddClients(this IServiceCollection services)
    {
        return services;
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