using System;
using System.Threading.Tasks;
using DAP.SqlNotebook.Contract.Entities;

namespace DAP.SqlNotebook.UI;

/// <summary>
/// Service for inserting catalog table name or SELECT text into the editor.
/// Notebook page registers callbacks; SchemaTree uses the service when user picks a table.
/// </summary>
public interface ICatalogInsertService
{
    void SetInsertNameCallback(Func<string, Task>? callback);
    void SetInsertSelectCallback(Func<CatalogNodeInfo, Task>? callback);
    void SetInsertDescriptionCallback(Func<CatalogNodeInfo, Task>? callback);
    void SetInsertAtCursorCallback(Func<string, Task>? callback);
    /// <summary>Set by NotebookEdit: pastes full text into Monaco without triggering re-render.</summary>
    void SetPasteToEditorCallback(Func<string, Task>? callback);
    /// <summary>Set by Notebook page: updates _sqlEditorContent when pasting (no StateHasChanged).</summary>
    void SetOnContentPasted(Action<string>? callback);
    /// <summary>Number of rows for SELECT TOP N (e.g. Preview, Insert SELECT TOP). Default 10.</summary>
    int SelectTopCount { get; set; }
    Task InsertNameAsync(string name);
    Task InsertSelectAsync(CatalogNodeInfo node);
    Task InsertDescriptionAsync(CatalogNodeInfo node);
    Task InsertAtCursorAsync(string text);
    Task PasteToEditorAsync(string text);
}

public sealed class CatalogInsertService : ICatalogInsertService
{
    private Func<string, Task>? _insertName;
    private Func<CatalogNodeInfo, Task>? _insertSelect;
    private Func<CatalogNodeInfo, Task>? _insertDescription;
    private Func<string, Task>? _insertAtCursor;
    private Func<string, Task>? _pasteToEditor;
    private Action<string>? _onContentPasted;

    public int SelectTopCount { get; set; } = 10;

    public void SetInsertNameCallback(Func<string, Task>? callback) => _insertName = callback;
    public void SetInsertSelectCallback(Func<CatalogNodeInfo, Task>? callback) => _insertSelect = callback;
    public void SetInsertDescriptionCallback(Func<CatalogNodeInfo, Task>? callback) => _insertDescription = callback;
    public void SetInsertAtCursorCallback(Func<string, Task>? callback) => _insertAtCursor = callback;
    public void SetPasteToEditorCallback(Func<string, Task>? callback) => _pasteToEditor = callback;
    public void SetOnContentPasted(Action<string>? callback) => _onContentPasted = callback;

    public Task InsertNameAsync(string name)
    {
        return _insertName != null ? _insertName(name) : Task.CompletedTask;
    }

    public Task InsertSelectAsync(CatalogNodeInfo node)
    {
        return _insertSelect != null ? _insertSelect(node) : Task.CompletedTask;
    }

    public Task InsertDescriptionAsync(CatalogNodeInfo node)
    {
        return _insertDescription != null ? _insertDescription(node) : Task.CompletedTask;
    }

    public Task InsertAtCursorAsync(string text)
    {
        return _insertAtCursor != null ? _insertAtCursor(text) : Task.CompletedTask;
    }

    public async Task PasteToEditorAsync(string text)
    {
        _onContentPasted?.Invoke(text ?? "");
        if (_pasteToEditor != null)
            await _pasteToEditor(text ?? "");
    }
}
