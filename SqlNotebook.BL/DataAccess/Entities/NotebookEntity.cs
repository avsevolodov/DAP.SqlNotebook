using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DAP.SqlNotebook.BL.DataAccess.Entities
{
    [Table("Notebooks")]
    public class NotebookEntity
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        [MaxLength(512)]
        public string Name { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; }

        public DateTime UpdatedAt { get; set; }

        /// <summary>Windows/login who created the notebook.</summary>
        [MaxLength(256)]
        public string? CreatedBy { get; set; }

        /// <summary>Windows/login who last updated the notebook.</summary>
        [MaxLength(256)]
        public string? UpdatedBy { get; set; }

        /// <summary>Optional workspace this notebook belongs to.</summary>
        public Guid? WorkspaceId { get; set; }

        /// <summary>Default database (catalog node) for this notebook.</summary>
        public Guid? CatalogNodeId { get; set; }

        /// <summary>Display name of the default database for the UI.</summary>
        [MaxLength(256)]
        public string? CatalogNodeDisplayName { get; set; }

        /// <summary>Тип ноутбука: 0 = Db, 1 = Generic.</summary>
        public int NotebookType { get; set; }

        [ForeignKey(nameof(WorkspaceId))]
        public WorkspaceEntity? Workspace { get; set; }

        [InverseProperty(nameof(NotebookCellEntity.Notebook))]
        public virtual ICollection<NotebookCellEntity> Cells { get; set; } = new List<NotebookCellEntity>();
    }
}
