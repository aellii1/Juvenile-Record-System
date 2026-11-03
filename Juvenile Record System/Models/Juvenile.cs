using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Juvenile_Record_System.Models
{
    [Table("juveniles")]
    public class Juvenile
    {
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    [Column("juvenile_id")]
    public int JuvenileId { get; set; } // Display format: J-00482

    [Required(ErrorMessage = "Full name is required.")]
    [StringLength(100)]
    [Column("full_name")]
    public string FullName { get; set; }

    [Required(ErrorMessage = "Birth date is required.")]
    [Column("birth_date", TypeName = "date")]
    [DataType(DataType.Date)]
    public DateTime BirthDate { get; set; }

    [Required(ErrorMessage = "Address is required.")]
    [StringLength(255)]
    [Column("address")]
    public string Address { get; set; }

    [Required]
    [StringLength(20)]
    [Column("record_status")]
    public string RecordStatus { get; set; } = "Active"; // Active, Inactive, Archived

    [Column("created_by")]
    public int CreatedBy { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    [Column("updated_by")]
    public int? UpdatedBy { get; set; }

    [Column("updated_at")]
        public DateTime? UpdatedAt { get; set; }
    }
}