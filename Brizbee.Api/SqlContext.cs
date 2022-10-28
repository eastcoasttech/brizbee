using Brizbee.Core.Models;
using Microsoft.EntityFrameworkCore;

namespace Brizbee.Api
{
    public class SqlContext : DbContext
    {
        public SqlContext(DbContextOptions<SqlContext> options)
            : base(options)
        {
        }

        public DbSet<Commit>? Commits { get; set; }

        public DbSet<Customer>? Customers { get; set; }

        public DbSet<Job>? Jobs { get; set; }

        public DbSet<Organization>? Organizations { get; set; }

        public DbSet<Punch>? Punches { get; set; }

        public DbSet<QuickBooksDesktopExport>? QuickBooksDesktopExports { get; set; }

        public DbSet<QuickBooksOnlineExport>? QuickBooksOnlineExports { get; set; }

        public DbSet<QBDInventoryConsumption>? QBDInventoryConsumptions { get; set; }

        public DbSet<QBDInventoryConsumptionSync>? QBDInventoryConsumptionSyncs { get; set; }

        public DbSet<QBDInventoryItem>? QBDInventoryItems { get; set; }

        public DbSet<QBDInventoryItemSync>? QBDInventoryItemSyncs { get; set; }

        public DbSet<QBDInventorySite>? QBDInventorySites { get; set; }

        public DbSet<QBDUnitOfMeasureSet>? QBDUnitOfMeasureSets { get; set; }

        public DbSet<PopulateTemplate>? PopulateTemplates { get; set; }

        public DbSet<Rate>? Rates { get; set; }

        public DbSet<Brizbee.Core.Models.Task>? Tasks { get; set; }

        public DbSet<TaskTemplate>? TaskTemplates { get; set; }

        public DbSet<TimesheetEntry>? TimesheetEntries { get; set; }

        public DbSet<User>? Users { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Password is not a column, so should be ignored.
            modelBuilder.Entity<User>()
                .Ignore(u => u.Password);

            // Organization codes should be universally unique.
            modelBuilder.Entity<Organization>()
                .HasIndex(o => o.Code)
                .IsUnique();

            modelBuilder.Entity<Job>()
                .Ignore(j => j.TaskTemplateId);

            // Configure decimal precision.
            modelBuilder.Entity<QBDInventoryItem>()
                .Property(o => o.SalesPrice)
                .HasColumnType("DECIMAL (10,2)")
                .HasPrecision(10, 2);

            modelBuilder.Entity<QBDInventoryItem>()
                .Property(o => o.PurchaseCost)
                .HasColumnType("DECIMAL (10,2)")
                .HasPrecision(10, 2);
        }
    }
}
