using System;

namespace DAP.SqlNotebook.UI;

/// <summary>
/// Current notebook context (set by Notebook page) so that AI panel can load/save history per notebook.
/// </summary>
public interface ICurrentNotebookContext
{
    Guid? NotebookId { get; set; }
    event Action? NotebookIdChanged;
}

public sealed class CurrentNotebookContext : ICurrentNotebookContext
{
    private Guid? _notebookId;

    public Guid? NotebookId
    {
        get => _notebookId;
        set
        {
            if (_notebookId == value) return;
            _notebookId = value;
            NotebookIdChanged?.Invoke();
        }
    }

    public event Action? NotebookIdChanged;
}
