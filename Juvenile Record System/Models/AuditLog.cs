using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Juvenile_Record_System.Models
{
    [Table("audit_logs")]
    public class AuditLog
    {
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    [Column("audit_id")]
    public long AuditId { get; set; }

    [Column("user_id")]
    public int UserId { get; set; }

    [Required]
    [StringLength(100)]
    [Column("action")]
    public string Action { get; set; }

    [StringLength(50)]
    [Column("entity_type")]
    public string EntityType { get; set; }

    [StringLength(50)]
    [Column("entity_id")]
    public string EntityId { get; set; }

    [Column("details", TypeName = "text")]
    public string Details { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    [ForeignKey("UserId")]
        public virtual User User { get; set; }
    }
}