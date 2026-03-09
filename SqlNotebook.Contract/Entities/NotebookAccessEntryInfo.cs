using System.Text.Json.Serialization;

namespace DAP.SqlNotebook.Contract.Entities;

/// <summary>Single access entry (user + role) for a notebook.</summary>
public class NotebookAccessEntryInfo
{
    public string UserLogin { get; set; } = string.Empty;

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public NotebookAccessRoleInfo Role { get; set; } = NotebookAccessRoleInfo.Viewer;
}

