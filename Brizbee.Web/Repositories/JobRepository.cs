using Brizbee.Common.Models;
using Brizbee.Policies;
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
            return db.Jobs.Find(id);
        }

        /// <summary>
        /// Returns a queryable collection of jobs.
        /// </summary>
        /// <param name="currentUser">The user to check for permissions</param>
        /// <returns>The queryable collection of jobs</returns>
        public IQueryable<Job> GetAll(User currentUser)
        {
            return db.Jobs.AsQueryable<Job>();
        }
    }
}