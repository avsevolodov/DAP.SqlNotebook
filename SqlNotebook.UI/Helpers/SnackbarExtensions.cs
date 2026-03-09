using MudBlazor;

namespace DAP.SqlNotebook.UI.Helpers;

internal static class SnackbarExtensions
{
    public static void ShowPopupMessage(this ISnackbar snackbar, string message, Severity severity = Severity.Info)
    {
        snackbar.Add(message, severity);
    }
}