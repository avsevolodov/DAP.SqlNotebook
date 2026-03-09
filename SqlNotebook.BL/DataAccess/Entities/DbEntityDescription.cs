using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DAP.SqlNotebook.BL.DataAccess.Entities
{
    [Table("DbEntities")]
    public class DbEntityDescription
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        [MaxLength(256)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(512)]
        public string? DisplayName { get; set; }

        [MaxLength(256)]
        public string? SchemaName { get; set; }

        public string? Description { get; set; }
    }
}
