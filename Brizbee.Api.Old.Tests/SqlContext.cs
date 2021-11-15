using Brizbee.Common.Models;
using Microsoft.EntityFrameworkCore;

namespace Brizbee.Api.Tests
{
    public class SqlContext : DbContext
    {
        public SqlContext(DbContextOptions<SqlContext> options)
            : base(options)
        {
        }

        public DbSet<Organization> Organizations { get; set; }
        public DbSet<User> Users { get; set; }
        public DbSet<Customer> Customers { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Password is not a column, so should be ignored
            modelBuilder.Entity<User>()
                .Ignore(u => u.Password);

            // Organization codes should be universally unique
            modelBuilder.Entity<Organization>()
                .HasIndex(o => o.Code)
                .IsUnique();
        }
    }
}
