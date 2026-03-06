using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DAP.SqlNotebook.BL.DataAccess.Entities
{
    /// <summary>
    /// Cell type: 0 = Sql, 1 = Markdown, 2 = Chart.
    /// </summary>
    public enum NotebookCellTypeEntity
    {
        Sql = 0,
        Markdown = 1,
        Chart = 2,
    }

    [Table("NotebookCells")]
    public class NotebookCellEntity
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        public Guid NotebookId { get; set; }

        [ForeignKey(nameof(NotebookId))]
        public virtual NotebookEntity? Notebook { get; set; }

        /// <summary>
        /// Order of the cell within the notebook (0-based).
        /// </summary>
        public int OrderIndex { get; set; }

        public NotebookCellTypeEntity CellType { get; set; }

        [Required]
        public string Content { get; set; } = string.Empty;

        /// <summary>
        /// JSON-serialized execution result (NotebookCellExecutionResultInfo).
        /// </summary>
        public string? ExecutionResultJson { get; set; }

        /// <summary>
        /// Database (catalog node) used when this cell was executed.
        /// </summary>
        public Guid? CatalogNodeId { get; set; }

        /// <summary>
        /// Display name of the database for the UI.
        /// </summary>
        [MaxLength(256)]
        public string? DatabaseDisplayName { get; set; }

        [MaxLength(256)]
        public string? CreatedBy { get; set; }
    }
}
