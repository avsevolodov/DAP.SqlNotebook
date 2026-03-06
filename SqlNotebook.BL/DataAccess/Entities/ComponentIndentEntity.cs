using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DAP.SqlNotebook.BL.DataAccess.Entities
{
    public class ComponentIndentEntity
    {
        [Key]
        public string IntentId { get; set; } = null!;

        [Required]
        public string FromService { get; set; } = null!;

        [Required]
        public string FromComponent { get; set; } = null!;

        [Required]
        public string ToService { get; set; } = null!;

        [Required]
        public string ToComponent { get; set; } = null!;

        public string? Purpose { get; set; }

        public string? DataClassification { get; set; }

        public string? Status { get; set; }

        public DateTime CreatedAt { get; set; }

        public DateTime UpdatedAt { get; set; }
    }
}
