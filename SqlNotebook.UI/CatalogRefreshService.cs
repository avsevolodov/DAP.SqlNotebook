namespace DAP.SqlNotebook.UI;

/// <summary>
/// Notifies the schema catalog tree (e.g. in layout) to refresh when sources are added or deleted.
/// </summary>
public interface ICatalogRefreshService
{
    event System.Action? OnCatalogChanged;
    void NotifyRefresh();
}

public sealed class CatalogRefreshService : ICatalogRefreshService
{
    public event System.Action? OnCatalogChanged;

    public void NotifyRefresh() => OnCatalogChanged?.Invoke();
}
