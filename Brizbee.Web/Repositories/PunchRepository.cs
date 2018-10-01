using Brizbee.Common.Models;
using Brizbee.Policies;
using Microsoft.AspNet.OData;
using System;
using System.Linq;

namespace Brizbee.Repositories
{
    public class PunchRepository : IDisposable
    {
        private BrizbeeWebContext db = new BrizbeeWebContext();

        /// <summary>
        /// Creates the given punch in the database.
        /// </summary>
        /// <param name="punch">The punch to create</param>
        /// <param name="currentUser">The user to check for permissions</param>
        /// <returns>The created punch</returns>
        public Punch Create(Punch punch, User currentUser)
        {
            // Auto-generated
            punch.CreatedAt = DateTime.Now;
            punch.Guid = Guid.NewGuid();

            db.Punches.Add(punch);

            db.SaveChanges();

            return punch;
        }

        /// <summary>
        /// Deletes the punch with the given id.
        /// </summary>
        /// <param name="id">The id of the punch</param>
        /// <param name="currentUser">The user to check for permissions</param>
        public void Delete(int id, User currentUser)
        {
            var punch = db.Punches.Find(id);

            // Ensure that user is authorized
            if (!PunchPolicy.CanDelete(punch, currentUser))
            {
                throw new Exception("Not authorized to delete the object");
            }

            // Delete the object itself
            db.Punches.Remove(punch);

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
        /// Returns the punch with the given id.
        /// </summary>
        /// <param name="id">The id of the punch</param>
        /// <param name="currentUser">The user to check for permissions</param>
        /// <returns>The punch with the given id</returns>
        public Punch Get(int id, User currentUser)
        {
            var userIds = db.Users
                .Where(u => u.OrganizationId == currentUser.OrganizationId)
                .Select(u => u.Id);
            return db.Punches
                .Where(p => userIds.Contains(p.UserId))
                .Where(p => p.Id == id)
                .FirstOrDefault();
        }

        /// <summary>
        /// Returns a queryable collection of punches.
        /// </summary>
        /// <param name="currentUser">The user to check for permissions</param>
        /// <returns>The queryable collection of punches</returns>
        public IQueryable<Punch> GetAll(User currentUser)
        {
            var userIds = db.Users
                .Where(u => u.OrganizationId == currentUser.OrganizationId)
                .Select(u => u.Id);
            return db.Punches.Where(p => userIds.Contains(p.UserId))
                .AsQueryable<Punch>();
        }

        /// <summary>
        /// Updates the given punch with the given delta of changes.
        /// </summary>
        /// <param name="id">The id of the punch</param>
        /// <param name="patch">The changes that should be made to the punch</param>
        /// <param name="currentUser">The user to check for permissions</param>
        /// <returns>The updated punch</returns>
        public Punch Update(int id, Delta<Punch> patch, User currentUser)
        {
            var punch = db.Punches.Find(id);

            //// Ensure that object was found
            //ValidateFound(group, id);

            //// Ensure that user is authorized
            //if (!GroupPolicy.CanUpdate(currentUser, group))
            //{
            //    var resp = new HttpResponseMessage(HttpStatusCode.Forbidden)
            //    {
            //        Content = new StringContent("No permission to modify the group"),
            //        ReasonPhrase = "Permission Denied"
            //    };
            //    throw new HttpResponseException(resp);
            //}

            //// Do not allow modifying some properties
            //if (patch.GetChangedPropertyNames().Contains("OrganizationId"))
            //{
            //    var resp = new HttpResponseMessage(HttpStatusCode.Forbidden)
            //    {
            //        Content = new StringContent("Cannot modify OrganizationId"),
            //        ReasonPhrase = "Permission Denied"
            //    };
            //    throw new HttpResponseException(resp);
            //}

            // Peform the update
            patch.Patch(punch);
            
            db.SaveChanges();

            return punch;
        }

        /// <summary>
        /// Creates a punch in the database for the given user with the
        /// given task and current timestamp.
        /// </summary>
        /// <param name="taskId">The id of the task</param>
        /// <param name="currentUser">The user to punch in</param>
        public void PunchIn(int taskId, User currentUser)
        {
            var punch = new Punch();
            var now = DateTime.Now;

            // Auto-generated
            punch.CreatedAt = DateTime.Now;
            punch.InAt = new DateTime(now.Year, now.Month, now.Day, now.Hour, now.Minute, 0);
            punch.TaskId = taskId;
            punch.UserId = currentUser.Id;

            db.Punches.Add(punch);

            db.SaveChanges();
        }

        /// <summary>
        /// Finds the most recent punch in the database which does not
        /// have an "out" timestamp, and updates the punch's out value
        /// to be the current timestamp.
        /// </summary>
        /// <param name="currentUser">The user to punch out</param>
        public void PunchOut(User currentUser)
        {
            var punch = db.Punches.Where(p => p.UserId == currentUser.Id)
                .Where(p => p.OutAt == null)
                .OrderByDescending(p => p.InAt)
                .FirstOrDefault();
            var now = DateTime.Now;

            punch.OutAt = new DateTime(now.Year, now.Month, now.Day, now.Hour, now.Minute, 59);

            db.SaveChanges();
        }
    }
}