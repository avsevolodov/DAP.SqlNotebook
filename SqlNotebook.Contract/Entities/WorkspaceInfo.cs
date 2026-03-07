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
}
