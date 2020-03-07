using Brizbee.Common.Models;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Text;

namespace Brizbee.Common.Database
{
    public interface ISqlContext : IDisposable
    {
        DbSet<Commit> Commits { get; }
        DbSet<Customer> Customers { get; }
        DbSet<Job> Jobs { get; }
        DbSet<Organization> Organizations { get; }
        DbSet<Punch> Punches { get; }
        DbSet<QuickBooksDesktopExport> QuickBooksDesktopExports { get; }
        DbSet<QuickBooksOnlineExport> QuickBooksOnlineExports { get; }
        DbSet<Rate> Rates { get; }
        DbSet<Task> Tasks { get; }
        DbSet<TaskTemplate> TaskTemplates { get; }
        DbSet<TimesheetEntry> TimesheetEntries { get; }
        DbSet<User> Users { get; }
        int SaveChanges();
        void MarkAsModified(object obj);
    }
}
