using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DAP.SqlNotebook.BL.DataAccess.Entities
{
    [Table("AiAssistMessages")]
    public class AiAssistMessageEntity
    {
        [Key]
        public Guid Id { get; set; }

        /// <summary>Optional: notebook context when generating SQL (for "insert into editor").</summary>
        public Guid? NotebookId { get; set; }

        /// <summary>User login for global chat (messages are per user, not per notebook).</summary>
        [MaxLength(256)]
        public string? UserLogin { get; set; }

        /// <summary>Chat session (null = legacy global stream).</summary>
        public Guid? SessionId { get; set; }

        public string? Content { get; set; }
        /// <summary>0 = User, 1 = Assistant</summary>
        public int Role { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
