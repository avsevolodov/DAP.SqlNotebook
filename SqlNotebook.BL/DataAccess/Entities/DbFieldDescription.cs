using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DAP.SqlNotebook.BL.DataAccess.Entities
{
    [Table("DbFields")]
    public class DbFieldDescription
    {
        [Key]
        public Guid Id { get; set; }

        public Guid EntityId { get; set; }

        [Required]
        [MaxLength(256)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(256)]
        public string? DataType { get; set; }

        public bool IsNullable { get; set; }

        public bool IsPrimaryKey { get; set; }

        public string? Description { get; set; }

        [ForeignKey(nameof(EntityId))]
        public DbEntityDescription? Entity { get; set; }
    }
}
