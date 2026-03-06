using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DAP.SqlNotebook.BL.DataAccess.Entities
{
    [Table("Workspaces")]
    public class WorkspaceEntity
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        [MaxLength(256)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(2048)]
        public string? Description { get; set; }

        /// <summary>Windows/login of the workspace owner. Null = legacy (visible to all).</summary>
        [MaxLength(256)]
        public string? OwnerLogin { get; set; }
    }
}
