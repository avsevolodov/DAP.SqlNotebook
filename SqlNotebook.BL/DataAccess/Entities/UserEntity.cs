using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DAP.SqlNotebook.BL.DataAccess.Entities
{
    [Table("Users")]
    public class UserEntity
    {
        [Key]
        public Guid Id { get; set; }

        [MaxLength(256)]
        public string? Login { get; set; }

        [MaxLength(32)]
        public string? Role { get; set; }
    }
}
