using Brizbee.Common.Models;
using Brizbee.Policies;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Brizbee.Repositories
{
    public class TaskRepository : IDisposable
    {
        private BrizbeeWebContext db = new BrizbeeWebContext();

        /// <summary>
        /// Creates the given task in the database.
        /// </summary>
        /// <param name="task">The task to create</param>
        /// <param name="currentUser">The user to check for permissions</param>
        /// <returns>The created task</returns>
        public Task Create(Task task, User currentUser)
        {
            var job = db.Jobs.Find(task.JobId);

            // Auto-generated
            task.CreatedAt = DateTime.Now;
            task.JobId = job.Id;

            db.Tasks.Add(task);

            db.SaveChanges();

            return task;
        }

        /// <summary>
        /// Deletes the task with the given id.
        /// </summary>
        /// <param name="id">The id of the task</param>
        /// <param name="currentUser">The user to check for permissions</param>
        public void Delete(int id, User currentUser)
        {
            var task = db.Tasks.Find(id);

            // Ensure that user is authorized
            if (!TaskPolicy.CanDelete(task, currentUser))
            {
                throw new Exception("Not authorized to delete the object");
            }

            // Delete the object itself
            db.Tasks.Remove(task);
            
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
        /// Returns the task with the given id.
        /// </summary>
        /// <param name="id">The id of the task</param>
        /// <param name="currentUser">The user to check for permissions</param>
        /// <returns>The task with the given id</returns>
        public Task Get(int id, User currentUser)
        {
            return db.Tasks.Find(id);
        }

        /// <summary>
        /// Returns a queryable collection of tasks.
        /// </summary>
        /// <param name="currentUser">The user to check for permissions</param>
        /// <returns>The queryable collection of tasks</returns>
        public IQueryable<Task> GetAll(User currentUser)
        {
            return db.Tasks.AsQueryable<Task>();
        }
    }
}