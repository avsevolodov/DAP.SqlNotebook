namespace DAP.SqlNotebook.UI;

/// <summary>
/// Holds the current editor SQL so AI Assist can use it as context when generating.
/// Set by Notebook page when editor content changes.
/// </summary>
public interface ICurrentSqlContext
{
    string? CurrentSql { get; set; }
}

public sealed class CurrentSqlContext : ICurrentSqlContext
{
    public string? CurrentSql { get; set; }
}
