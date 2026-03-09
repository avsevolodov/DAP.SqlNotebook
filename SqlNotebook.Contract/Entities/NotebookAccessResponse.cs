namespace DAP.SqlNotebook.Contract.Entities;

public class NotebookAccessResponse
{
    public List<NotebookAccessEntryInfo> Entries { get; set; } = new();
}

