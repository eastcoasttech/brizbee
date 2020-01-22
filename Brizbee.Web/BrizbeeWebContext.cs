using Brizbee.Common.Models;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.Infrastructure.Interception;
using System.Linq;
using System.Linq.Expressions;
using System.Web;

namespace Brizbee.Web
{
    public class BrizbeeWebContext : DbContext
    {
        static BrizbeeWebContext()
        {
        }

        public BrizbeeWebContext() : base("name=BrizbeeWebContext")
        {
            this.Configuration.LazyLoadingEnabled = false;
        }

        // Add a DbSet for each entity type that you want to include in your model. For more information 
        // on configuring and using a Code First model, see http://go.microsoft.com/fwlink/?LinkId=390109.
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
        
        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            //base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<User>().Ignore(u => u.Password);
            modelBuilder.Entity<Organization>()
                .HasIndex(o => o.Code)
                .IsUnique();
        }
    }
}