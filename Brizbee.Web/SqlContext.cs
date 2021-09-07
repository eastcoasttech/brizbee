//
//  SqlContext.cs
//  BRIZBEE API
//
//  Copyright (C) 2020 East Coast Technology Services, LLC
//
//  This file is part of the BRIZBEE API.
//
//  This program is free software: you can redistribute it and/or modify
//  it under the terms of the GNU Affero General Public License as
//  published by the Free Software Foundation, either version 3 of the
//  License, or (at your option) any later version.
//
//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//  GNU Affero General Public License for more details.
//
//  You should have received a copy of the GNU Affero General Public License
//  along with this program.  If not, see <https://www.gnu.org/licenses/>.
//

using Brizbee.Common.Models;
using System.Data.Entity;

namespace Brizbee.Web
{
    public class SqlContext : DbContext
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
        public DbSet<QBDInventoryConsumption> QBDInventoryConsumptions { get; set; }
        public DbSet<QBDInventoryConsumptionSync> QBDInventoryConsumptionSyncs { get; set; }
        public DbSet<QBDInventoryItem> QBDInventoryItems { get; set; }
        public DbSet<QBDInventoryItemSync> QBDInventoryItemSyncs { get; set; }
        public DbSet<QBDInventorySite> QBDInventorySites { get; set; }
        public DbSet<QBDUnitOfMeasureSet> QBDUnitOfMeasureSets { get; set; }
        public DbSet<PopulateTemplate> PopulateTemplates { get; set; }
        public DbSet<Rate> Rates { get; set; }
        public DbSet<Task> Tasks { get; set; }
        public DbSet<TaskTemplate> TaskTemplates { get; set; }
        public DbSet<TimesheetEntry> TimesheetEntries { get; set; }
        public DbSet<User> Users { get; set; }

        protected override void OnModelCreating(DbModelBuilder mb)
        {
            // Password is not a column, so should be ignored
            mb.Entity<User>().Ignore(u => u.Password);

            // TaskTemplateId is not a column, so should be ignored
            mb.Entity<Job>().Ignore(j => j.TaskTemplateId);

            // Organization codes should be universally unique
            mb.Entity<Organization>();
                //.HasIndex(o => o.Code)
                //.IsUnique();
        }

        public void MarkAsModified(object obj)
        {
            Entry(obj).State = EntityState.Modified;
        }
    }
}