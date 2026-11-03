using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Juvenile_Record_System.Models
{
    [Table("roles")]
    public class Role
    {
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    [Column("role_id")]
    public int RoleId { get; set; }

    [Required]
    [StringLength(50)]
    [Column("role_name")]
    public string RoleName { get; set; }

    [Required]
    [StringLength(20)]
    [Column("access_status")]
    public string AccessStatus { get; set; } = "Enabled";

    [Column("session_timeout_min")]
    public int SessionTimeoutMin { get; set; } = 15;

    [Column("failed_login_limit")]
    public int FailedLoginLimit { get; set; } = 5;

    [StringLength(100)]
    [Column("access_scope")]
        public string AccessScope { get; set; }
    }
}