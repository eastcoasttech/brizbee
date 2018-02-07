using Brizbee.Common.Models;
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
        public Task Create(Task task, Job job, User currentUser)
        {
            // Auto-generated
            task.CreatedAt = DateTime.Now;
            task.JobId = job.Id;

            db.Tasks.Add(task);

            db.SaveChanges();

            return task;
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