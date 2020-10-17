using Brizbee.Common.Models;
using Brizbee.Web.Policies;
using Microsoft.AspNet.OData;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Brizbee.Web.Repositories
{
    public class TaskTemplateRepository : IDisposable
    {
        private SqlContext db = new SqlContext();

        /// <summary>
        /// Creates the given task template in the database.
        /// </summary>
        /// <param name="taskTemplate">The task template to create</param>
        /// <param name="currentUser">The user to check for permissions</param>
        /// <returns>The created task template</returns>
        public TaskTemplate Create(TaskTemplate taskTemplate, User currentUser)
        {
            // Ensure that user is authorized
            if (!TaskTemplatePolicy.CanCreate(taskTemplate, currentUser))
            {
                throw new Exception("Not authorized to create the object");
            }

            // Auto-generated
            taskTemplate.CreatedAt = DateTime.UtcNow;
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

            // Ensure that user is authorized
            if (!TaskTemplatePolicy.CanDelete(taskTemplate, currentUser))
            {
                throw new Exception("Not authorized to delete the object");
            }

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
        public IQueryable<TaskTemplate> Get(int id, User currentUser)
        {
            return db.TaskTemplates
                .Where(t => t.OrganizationId == currentUser.OrganizationId)
                .Where(t => t.Id == id);
        }

        /// <summary>
        /// Returns a queryable collection of task templates.
        /// </summary>
        /// <param name="currentUser">The user to check for permissions</param>
        /// <returns>The queryable collection of task templates</returns>
        public IQueryable<TaskTemplate> GetAll(User currentUser)
        {
            return db.TaskTemplates
                .Where(t => t.OrganizationId == currentUser.OrganizationId);
        }
    }
}