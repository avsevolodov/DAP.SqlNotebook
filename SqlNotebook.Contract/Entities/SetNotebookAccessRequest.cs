namespace DAP.SqlNotebook.Contract.Entities;

public class SetNotebookAccessRequest
{
    public List<NotebookAccessEntryInfo> Entries { get; set; } = new();
}

