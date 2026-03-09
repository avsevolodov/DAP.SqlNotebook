using DAP.SqlNotebook.Contract.Entities;

namespace DAP.SqlNotebook.UI;

/// <summary>
/// Holds the currently selected database (catalog node) for the editor.
/// Used to prepend USE [DatabaseName] when running and when inserting from find-tables.
/// </summary>
public interface ICurrentDatabaseContext
{
    CatalogNodeInfo? SelectedDatabase { get; set; }
}

public sealed class CurrentDatabaseContext : ICurrentDatabaseContext
{
    public CatalogNodeInfo? SelectedDatabase { get; set; }
}
