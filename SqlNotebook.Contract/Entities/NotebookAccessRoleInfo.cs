using System.Runtime.Serialization;

namespace DAP.SqlNotebook.Contract.Entities;

/// <summary>Notebook access role for a shared notebook.</summary>
public enum NotebookAccessRoleInfo
{
    /// <summary>Can view the notebook.</summary>
    [EnumMember(Value = @"Viewer")]
    Viewer = 0,

    /// <summary>Can view and edit the notebook.</summary>
    [EnumMember(Value = @"Editor")]
    Editor = 1,
}

