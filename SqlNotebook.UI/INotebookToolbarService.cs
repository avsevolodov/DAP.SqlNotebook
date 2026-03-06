using System;
using System.Threading.Tasks;

namespace DAP.SqlNotebook.UI;

/// <summary>
/// Registers notebook editor toolbar actions so they can be shown in the right panel.
/// </summary>
public interface INotebookToolbarService
{
    bool HasActions { get; }
    int? TimeoutSeconds { get; set; }
    event Action? Changed;

    void SetActions(Func<Task> run, Func<Task> runAsChart, Func<Task> addMarkdown, Func<int?, Task> onTimeoutChanged, int? initialTimeoutSeconds);
    void SetEditorContentProvider(Func<Task<string?>> getEditorContent);
    void ClearActions();
    Task<string?> GetEditorContentAsync();
    Task RunAsync();
    Task RunAsChartAsync();
    Task AddMarkdownAsync();
    Task SetTimeoutAsync(int? seconds);
}
