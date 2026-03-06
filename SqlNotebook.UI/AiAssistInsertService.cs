using System;

namespace DAP.SqlNotebook.UI;

/// <summary>
/// Allows AI Assist panel to insert generated SQL into the current notebook editor.
/// Notebook page registers a callback when opened; the panel calls InsertIntoEditor when user clicks Insert.
/// </summary>
public interface IAiAssistInsertService
{
    void SetInsertCallback(Action<string>? callback);
    void InsertIntoEditor(string sql);
}

public sealed class AiAssistInsertService : IAiAssistInsertService
{
    private Action<string>? _callback;

    public void SetInsertCallback(Action<string>? callback)
    {
        _callback = callback;
    }

    public void InsertIntoEditor(string sql)
    {
        _callback?.Invoke(sql ?? string.Empty);
    }
}
