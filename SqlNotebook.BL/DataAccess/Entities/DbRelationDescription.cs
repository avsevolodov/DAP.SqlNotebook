using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DAP.SqlNotebook.BL.DataAccess.Entities
{
    [Table("DbRelations")]
    public class DbRelationDescription
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        [MaxLength(256)]
        public string FromFieldName { get; set; } = string.Empty;

        [Required]
        [MaxLength(256)]
        public string ToFieldName { get; set; } = string.Empty;

        [MaxLength(512)]
        public string? Name { get; set; }

        public string? Description { get; set; }
    }
}
