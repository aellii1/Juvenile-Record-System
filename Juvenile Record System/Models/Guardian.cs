using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Juvenile_Record_System.Models
{
    [Table("guardians")]
    public class Guardian
    {
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    [Column("guardian_id")]
    public int GuardianId { get; set; }

    [Column("juvenile_id")]
    public int JuvenileId { get; set; }

    [Required(ErrorMessage = "Guardian full name is required.")]
    [StringLength(100)]
    [Column("full_name")]
    public string FullName { get; set; }

    [StringLength(50)]
    [Column("relationship")]
    public string Relationship { get; set; }

    [StringLength(30)]
    [Column("contact_no")]
    public string ContactNo { get; set; }

    [ForeignKey("JuvenileId")]
        public virtual Juvenile Juvenile { get; set; }
    }
}