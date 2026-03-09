using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DAP.SqlNotebook.BL.DataAccess.Entities
{
    public enum DataMartNodeType
    {
        Folder = 0,
        Database = 1,
        Table = 2,
        Topic = 3,
    }

    [Table("DataMartNodes")]
    public class DataMartNodeEntity
    {
        [Key]
        public Guid Id { get; set; }

        public Guid? ParentId { get; set; }

        public DataMartNodeType Type { get; set; }

        [Required]
        [MaxLength(256)]
        public string Name { get; set; } = string.Empty;

        public string? Description { get; set; }

        [MaxLength(256)]
        public string? Owner { get; set; }

        [MaxLength(64)]
        public string? Provider { get; set; }

        [MaxLength(2048)]
        public string? ConnectionInfo { get; set; }

        [MaxLength(256)]
        public string? DatabaseName { get; set; }

        /// <summary>Auth mode: "Basic" (login/password) or "Kerberos" (Integrated Security).</summary>
        [MaxLength(32)]
        public string? AuthType { get; set; }

        [MaxLength(256)]
        public string? Login { get; set; }

        /// <summary>Encrypted password for Basic auth; null for Kerberos.</summary>
        [MaxLength(2048)]
        public string? PasswordEncrypted { get; set; }

        /// <summary>Kafka: consumer group id prefix (used as-is when AutoGenerate is false).</summary>
        [MaxLength(256)]
        public string? ConsumerGroupPrefix { get; set; }

        /// <summary>Kafka: when true, a GUID suffix is appended each call.</summary>
        public bool ConsumerGroupAutoGenerate { get; set; }

        public int SortOrder { get; set; }

        public Guid? EntityId { get; set; }

        [ForeignKey(nameof(ParentId))]
        public DataMartNodeEntity? Parent { get; set; }

        [ForeignKey(nameof(EntityId))]
        public DbEntityDescription? Entity { get; set; }
    }
}
