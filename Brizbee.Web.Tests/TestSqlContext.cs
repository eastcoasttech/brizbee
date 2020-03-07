using Brizbee.Common.Database;
using Brizbee.Common.Models;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Text;

namespace Brizbee.Web.Tests
{
    public class TestSqlContext : ISqlContext
    {
        public TestSqlContext()
        {
            this.Punches = new TestPunchDbSet();
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

        public int SaveChanges()
        {
            return 0;
        }

        public void MarkAsModified(object item) { }
        public void Dispose() { }
    }
}
