using System;

namespace DAP.SqlNotebook.UI;

public interface INotebookRunContext
{
    Guid? NotebookId { get; set; }
    int? ActiveCellIndex { get; set; }
    bool IsRunAllRunning { get; set; }
    event Action? StateChanged;
}

public sealed class NotebookRunContext : INotebookRunContext
{
    private Guid? _notebookId;
    private int? _activeCellIndex;
    private bool _isRunAllRunning;

    public Guid? NotebookId
    {
        get => _notebookId;
        set
        {
            if (_notebookId == value) return;
            _notebookId = value;
            OnStateChanged();
        }
    }

    public int? ActiveCellIndex
    {
        get => _activeCellIndex;
        set
        {
            if (_activeCellIndex == value) return;
            _activeCellIndex = value;
            OnStateChanged();
        }
    }

    public bool IsRunAllRunning
    {
        get => _isRunAllRunning;
        set
        {
            if (_isRunAllRunning == value) return;
            _isRunAllRunning = value;
            OnStateChanged();
        }
    }

    public event Action? StateChanged;

    private void OnStateChanged() => StateChanged?.Invoke();
}

