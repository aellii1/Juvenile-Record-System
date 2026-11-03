using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Juvenile_Record_System.Models
{
    [Table("users")]
    public class User
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [Column("user_id")]
        public int UserId { get; set; }

        [Required]
        [StringLength(100)]
        [Column("full_name")]
        public string FullName { get; set; }

        [Required]
        [StringLength(50)]
        [Column("username")]
        public string Username { get; set; }

        [Required]
        [StringLength(255)]
        [Column("password_hash")]
        public string PasswordHash { get; set; }

        [Column("role_id")]
        public int RoleId { get; set; }

        [Required]
        [StringLength(20)]
        [Column("account_status")]
        public string AccountStatus { get; set; } = "Active";

        [Column("failed_login_count")]
        public int FailedLoginCount { get; set; } = 0;

        [Column("last_login")]
        public DateTime? LastLogin { get; set; }

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        [ForeignKey("RoleId")]
        public virtual Role Role { get; set; }

        // Helper property to display IDs like U-001, U-002, etc.
        [NotMapped]
        public string FormattedUserId => $"U-{UserId:D3}";
    }
}