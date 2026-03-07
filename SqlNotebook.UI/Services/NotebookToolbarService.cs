using System;
using System.Threading.Tasks;

namespace DAP.SqlNotebook.UI;

public sealed class NotebookToolbarService : INotebookToolbarService
{
    private Func<Task>? _run;
    private Func<Task>? _runAsChart;
    private Func<Task>? _addMarkdown;
    private Func<int?, Task>? _onTimeoutChanged;
    private Func<Task<string?>>? _getEditorContent;

    public bool HasActions => _run != null;
    public int? TimeoutSeconds { get; set; }
    public event Action? Changed;

    public void SetActions(Func<Task> run, Func<Task> runAsChart, Func<Task> addMarkdown, Func<int?, Task> onTimeoutChanged, int? initialTimeoutSeconds)
    {
        _run = run;
        _runAsChart = runAsChart;
        _addMarkdown = addMarkdown;
        _onTimeoutChanged = onTimeoutChanged;
        TimeoutSeconds = initialTimeoutSeconds;
        Changed?.Invoke();
    }

    public void SetEditorContentProvider(Func<Task<string?>> getEditorContent)
    {
        _getEditorContent = getEditorContent;
    }

    public void ClearActions()
    {
        _run = null;
        _runAsChart = null;
        _addMarkdown = null;
        _onTimeoutChanged = null;
        _getEditorContent = null;
        Changed?.Invoke();
    }

    public async Task<string?> GetEditorContentAsync()
    {
        if (_getEditorContent == null) return null;
        return await _getEditorContent();
    }

    public async Task RunAsync()
    {
        if (_run != null) await _run();
    }

    public async Task RunAsChartAsync()
    {
        if (_runAsChart != null) await _runAsChart();
    }

    public async Task AddMarkdownAsync()
    {
        if (_addMarkdown != null) await _addMarkdown();
    }

    public async Task SetTimeoutAsync(int? seconds)
    {
        TimeoutSeconds = seconds;
        if (_onTimeoutChanged != null) await _onTimeoutChanged(seconds);
        Changed?.Invoke();
    }
}
