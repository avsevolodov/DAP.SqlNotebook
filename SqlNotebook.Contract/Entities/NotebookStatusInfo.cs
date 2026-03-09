using System.Runtime.Serialization;

namespace DAP.SqlNotebook.Contract.Entities;

/// <summary>Notebook lifecycle status: Active (default), Archived, or Trash (soft delete).</summary>
public enum NotebookStatusInfo
{
    [EnumMember(Value = @"Active")]
    Active = 0,

    [EnumMember(Value = @"Archived")]
    Archived = 1,

    [EnumMember(Value = @"Trash")]
    Trash = 2,
}
