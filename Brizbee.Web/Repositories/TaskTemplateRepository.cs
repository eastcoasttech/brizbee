using Brizbee.Common.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Brizbee.Repositories
{
    public class TaskTemplateRepository : IDisposable
    {
        private BrizbeeWebContext db = new BrizbeeWebContext();

        /// <summary>
        /// Creates the given task template in the database.
        /// </summary>
        /// <param name="taskTemplate">The task template to create</param>
        /// <param name="currentUser">The user to check for permissions</param>
        /// <returns>The created task template</returns>
        public TaskTemplate Create(TaskTemplate taskTemplate, User currentUser)
        {
            // Auto-generated
            taskTemplate.CreatedAt = DateTime.Now;
            taskTemplate.OrganizationId = currentUser.OrganizationId;

            db.TaskTemplates.Add(taskTemplate);

            db.SaveChanges();

            return taskTemplate;
        }

        /// <summary>
        /// Deletes the task template with the given id.
        /// </summary>
        /// <param name="id">The id of the task template</param>
        /// <param name="currentUser">The user to check for permissions</param>
        public void Delete(int id, User currentUser)
        {
            var taskTemplate = db.TaskTemplates.Find(id);

            //// Ensure that user is authorized
            //if (!TaskPolicy.CanDelete(task, currentUser))
            //{
            //    throw new Exception("Not authorized to delete the object");
            //}

            // Delete the object itself
            db.TaskTemplates.Remove(taskTemplate);

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
        /// Returns the task template with the given id.
        /// </summary>
        /// <param name="id">The id of the task template</param>
        /// <param name="currentUser">The user to check for permissions</param>
        /// <returns>The task template with the given id</returns>
        public TaskTemplate Get(int id, User currentUser)
        {
            return db.TaskTemplates.Find(id);
        }

        /// <summary>
        /// Returns a queryable collection of task templates.
        /// </summary>
        /// <param name="currentUser">The user to check for permissions</param>
        /// <returns>The queryable collection of task templates</returns>
        public IQueryable<TaskTemplate> GetAll(User currentUser)
        {
            return db.TaskTemplates.AsQueryable<TaskTemplate>();
        }
    }
}