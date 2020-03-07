using Brizbee.Common.Models;
using System.Data.Entity;

namespace Brizbee.Common.Database
{
    public class SqlContext : DbContext, ISqlContext
    {
        static SqlContext()
        {
        }

        public SqlContext() : base("name=SqlContext")
        {
            // Here is where to specify connection configuration
        }

        public DbSet<Commit> Commits { get; set; }
        public DbSet<Customer> Customers { get; set; }
        public DbSet<Job> Jobs { get; set; }
        public DbSet<Organization> Organizations { get; set; }
        public DbSet<Punch> Punches { get; set; }
        public DbSet<QuickBooksDesktopExport> QuickBooksDesktopExports { get; set; }
        public DbSet<QuickBooksOnlineExport> QuickBooksOnlineExports { get; set; }
        public DbSet<Rate> Rates { get; set; }
        public DbSet<Task> Tasks { get; set; }
        public DbSet<TaskTemplate> TaskTemplates { get; set; }
        public DbSet<TimesheetEntry> TimesheetEntries { get; set; }
        public DbSet<User> Users { get; set; }

        protected override void OnModelCreating(DbModelBuilder mb)
        {
            // Password is not a column, so should be ignored
            mb.Entity<User>().Ignore(u => u.Password);

            // Organization codes should be universally unique
            mb.Entity<Organization>()
                .HasIndex(o => o.Code)
                .IsUnique();
        }

        public void MarkAsModified(object obj)
        {
            Entry(obj).State = EntityState.Modified;
        }
    }
}
