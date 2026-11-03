using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Juvenile_Record_System.Models
{
    [Table("violations")]
    public class Violation
    {
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    [Column("violation_id")]
    public int ViolationId { get; set; }

    [Column("juvenile_id")]
    public int JuvenileId { get; set; }

    [Column("violation_date", TypeName = "date")]
    [DataType(DataType.Date)]
    public DateTime ViolationDate { get; set; }

    [Required]
    [Column("description", TypeName = "text")]
    public string Description { get; set; }

    [Required]
    [StringLength(20)]
    [Column("status")]
    public string Status { get; set; } = "Recorded";

    [Column("recorded_by")]
    public int RecordedBy { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    [ForeignKey("JuvenileId")]
    public virtual Juvenile Juvenile { get; set; }

    [ForeignKey("RecordedBy")]
        public virtual User Recorder { get; set; }
    }
}