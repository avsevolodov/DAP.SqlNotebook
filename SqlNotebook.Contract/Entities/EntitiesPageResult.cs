namespace DAP.SqlNotebook.Contract.Entities;

public class EntitiesPageResult
{
    public List<DbEntityInfo> Items { get; set; } = new();
    public int TotalCount { get; set; }
}
