using System.Text.Json.Serialization;

namespace DAP.SqlNotebook.Contract.Entities;

public class WorkspaceInfo
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? OwnerLogin { get; set; }
    /// <summary>Parent folder id. Null = root level.</summary>
    public Guid? ParentId { get; set; }
    /// <summary>True = folder (container), false = workspace (notebooks).</summary>
    public bool IsFolder { get; set; }
    /// <summary>Optional icon name (e.g. Material icon or emoji).</summary>
    public string? Icon { get; set; }
    /// <summary>Visibility: Private (only owner) or Public.</summary>
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public WorkspaceVisibilityInfo Visibility { get; set; } = WorkspaceVisibilityInfo.Private;
}
