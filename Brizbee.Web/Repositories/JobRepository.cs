using Brizbee.Common.Models;
using Brizbee.Policies;
using Microsoft.AspNet.OData;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Brizbee.Repositories
{
    public class JobRepository : IDisposable
    {
        private BrizbeeWebContext db = new BrizbeeWebContext();

        /// <summary>
        /// Creates the given job in the database.
        /// </summary>
        /// <param name="job">The job to create</param>
        /// <param name="currentUser">The user to check for permissions</param>
        /// <returns>The created job</returns>
        public Job Create(Job job, User currentUser)
        {
            var customer = db.Customers.Find(job.CustomerId);

            // Auto-generated
            job.CreatedAt = DateTime.Now;
            job.CustomerId = customer.Id;

            db.Jobs.Add(job);

            db.SaveChanges();

            return job;
        }

        /// <summary>
        /// Deletes the job with the given id.
        /// </summary>
        /// <param name="id">The id of the job</param>
        /// <param name="currentUser">The user to check for permissions</param>
        public void Delete(int id, User currentUser)
        {
            var job = db.Jobs.Find(id);

            // Ensure that user is authorized
            if (!JobPolicy.CanDelete(job, currentUser))
            {
                throw new Exception("Not authorized to delete the object");
            }

            // Delete the object itself
            db.Jobs.Remove(job);

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
        /// Returns the job with the given id.
        /// </summary>
        /// <param name="id">The id of the job</param>
        /// <param name="currentUser">The user to check for permissions</param>
        /// <returns>The job with the given id</returns>
        public Job Get(int id, User currentUser)
        {
            var customerIds = db.Customers
                .Where(c => c.OrganizationId == currentUser.OrganizationId)
                .Select(c => c.Id);
            return db.Jobs
                .Where(j => customerIds.Contains(j.CustomerId))
                .Where(j => j.Id == id)
                .FirstOrDefault();
        }

        /// <summary>
        /// Returns a queryable collection of jobs.
        /// </summary>
        /// <param name="currentUser">The user to check for permissions</param>
        /// <returns>The queryable collection of jobs</returns>
        public IQueryable<Job> GetAll(User currentUser)
        {
            var customerIds = db.Customers
                .Where(c => c.OrganizationId == currentUser.OrganizationId)
                .Select(c => c.Id);
            return db.Jobs
                .Where(j => customerIds.Contains(j.CustomerId))
                .AsQueryable<Job>();
        }

        /// <summary>
        /// Updates the given job with the given delta of changes.
        /// </summary>
        /// <param name="id">The id of the job</param>
        /// <param name="patch">The changes that should be made to the job</param>
        /// <param name="currentUser">The user to check for permissions</param>
        /// <returns>The updated job</returns>
        public Job Update(int id, Delta<Job> patch, User currentUser)
        {
            var job = db.Jobs.Find(id);

            // Ensure that object was found
            if (job == null) { throw new Exception("No object was found with that ID in the database"); }

            // Ensure that user is authorized
            if (!JobPolicy.CanUpdate(job, currentUser))
            {
                throw new Exception("Not authorized to modify the object");
            }

            // Do not allow modifying some properties
            if (patch.GetChangedPropertyNames().Contains("CustomerId"))
            {
                throw new Exception("Not authorized to modify the CustomerId");
            }

            // Peform the update
            patch.Patch(job);

            db.SaveChanges();

            return job;
        }
    }
}