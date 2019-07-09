using Brizbee.Common.Models;
using Brizbee.Web.Policies;
using Microsoft.AspNet.OData;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Brizbee.Web.Repositories
{
    public class TimesheetEntryRepository : IDisposable
    {
        private BrizbeeWebContext db = new BrizbeeWebContext();

        /// <summary>
        /// Creates the given timesheet entry in the database.
        /// </summary>
        /// <param name="timesheetEntry">The timesheet entry to create</param>
        /// <param name="currentUser">The user to check for permissions</param>
        /// <returns>The created timesheet entry</returns>
        public TimesheetEntry Create(TimesheetEntry timesheetEntry, User currentUser)
        {
            var now = DateTime.UtcNow;
            var organization = db.Organizations.Find(currentUser.OrganizationId);

            // Auto-generated
            timesheetEntry.CreatedAt = now;

            db.TimesheetEntries.Add(timesheetEntry);

            db.SaveChanges();

            return timesheetEntry;
        }

        /// <summary>
        /// Deletes the timesheet entry with the given id.
        /// </summary>
        /// <param name="id">The id of the timesheet entry</param>
        /// <param name="currentUser">The user to check for permissions</param>
        public void Delete(int id, User currentUser)
        {
            var timesheetEntry = db.TimesheetEntries.Find(id);

            // Ensure that user is authorized
            if (!TimesheetEntryPolicy.CanDelete(timesheetEntry, currentUser))
            {
                throw new Exception("Not authorized to delete the object");
            }

            // Delete the object itself
            db.TimesheetEntries.Remove(timesheetEntry);

            db.SaveChanges();
        }

        /// <summary>
        /// Disposes of the database connection.
        /// </summary>
        public void Dispose()
        {
            db.Dispose();
        }

        /// <summary>
        /// Returns the timesheet entry with the given id.
        /// </summary>
        /// <param name="id">The id of the timesheet entry</param>
        /// <param name="currentUser">The user to check for permissions</param>
        /// <returns>The timesheet entry with the given id</returns>
        public IQueryable<TimesheetEntry> Get(int id, User currentUser)
        {
            var userIds = db.Users
                .Where(u => u.OrganizationId == currentUser.OrganizationId)
                .Select(u => u.Id);
            return db.TimesheetEntries
                .Where(p => userIds.Contains(p.UserId))
                .Where(p => p.Id == id);
        }

        /// <summary>
        /// Returns a queryable collection of timesheet entries.
        /// </summary>
        /// <param name="currentUser">The user to check for permissions</param>
        /// <returns>The queryable collection of timesheet entries</returns>
        public IQueryable<TimesheetEntry> GetAll(User currentUser)
        {
            var userIds = db.Users
                .Where(u => u.OrganizationId == currentUser.OrganizationId)
                .Select(u => u.Id);
            return db.TimesheetEntries.Where(p => userIds.Contains(p.UserId));
        }

        /// <summary>
        /// Updates the given timesheet entry with the given delta of changes.
        /// </summary>
        /// <param name="id">The id of the timesheet entry</param>
        /// <param name="patch">The changes that should be made to the timesheet entry</param>
        /// <param name="currentUser">The user to check for permissions</param>
        /// <returns>The updated timesheet entry</returns>
        public TimesheetEntry Update(int id, Delta<TimesheetEntry> patch, User currentUser)
        {
            var timesheetEntry = db.TimesheetEntries.Find(id);
            
            // Peform the update
            patch.Patch(timesheetEntry);

            db.SaveChanges();

            return timesheetEntry;
        }
    }
}