using Brizbee.Common.Models;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.Infrastructure.Interception;
using System.Linq;
using System.Web;

namespace Brizbee
{
    public class BrizbeeWebContext : DbContext
    {
        static BrizbeeWebContext()
        {
        }

        // Context is configured to use the 'CollectorWebContext' connection string in the application's
        // configuration file (Web.config)
        public BrizbeeWebContext() : base("name=BrizbeeWebContext")
        {
        }

        // Add a DbSet for each entity type that you want to include in your model. For more information 
        // on configuring and using a Code First model, see http://go.microsoft.com/fwlink/?LinkId=390109.
        public DbSet<Commit> Commits { get; set; }
        public DbSet<Customer> Customers { get; set; }
        public DbSet<Job> Jobs { get; set; }
        public DbSet<Organization> Organizations { get; set; }
        public DbSet<Punch> Punches { get; set; }
        public DbSet<Task> Tasks { get; set; }
        public DbSet<User> Users { get; set; }
    }
}