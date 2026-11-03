using System.Data.Entity;

namespace Juvenile_Record_System.Models
{
    public class JuvenileContext : DbContext
    {
        public JuvenileContext() : base("name=JuvenileContext")
        {
        }

        public DbSet<Juvenile> Juveniles { get; set; }
        public DbSet<Guardian> Guardians { get; set; }
        public DbSet<User> Users { get; set; }
        public DbSet<Role> Roles { get; set; }
        public DbSet<Violation> Violations { get; set; }
        public DbSet<AuditLog> AuditLogs { get; set; }
    }
}