using System.Runtime.Serialization;

namespace DAP.SqlNotebook.Contract.Entities;

/// <summary>Workspace visibility: Private (only owner) or Public (visible to all).</summary>
public enum WorkspaceVisibilityInfo
{
    [EnumMember(Value = @"Private")]
    Private = 0,

    [EnumMember(Value = @"Public")]
    Public = 1,
}
