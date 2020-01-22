using Brizbee.Common.Exceptions;
using Brizbee.Common.Models;
using Brizbee.Web.Policies;
using Microsoft.AspNet.OData;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Brizbee.Web.Repositories
{
    public class RateRepository : IDisposable
    {
        private BrizbeeWebContext db = new BrizbeeWebContext();

        /// <summary>
        /// Creates the given rate in the database.
        /// </summary>
        /// <param name="rate">The rate to create</param>
        /// <param name="currentUser">The user to check for permissions</param>
        /// <returns>The created rate</returns>
        public Rate Create(Rate rate, User currentUser)
        {
            // Auto-generated
            rate.CreatedAt = DateTime.UtcNow;
            rate.OrganizationId = currentUser.OrganizationId;
            rate.IsDeleted = false;

            db.Rates.Add(rate);

            db.SaveChanges();

            return rate;
        }

        /// <summary>
        /// Deletes the rate with the given id.
        /// </summary>
        /// <param name="id">The id of the rate</param>
        /// <param name="currentUser">The user to check for permissions</param>
        public void Delete(int id, User currentUser)
        {
            var rate = db.Rates
                .Where(x => x.OrganizationId == currentUser.OrganizationId)
                .Where(x => x.IsDeleted == false)
                .Where(x => x.Id == id)
                .FirstOrDefault();

            // Mark the object as deleted
            rate.IsDeleted = true;
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
        /// Returns the rate with the given id.
        /// </summary>
        /// <param name="id">The id of the rate</param>
        /// <param name="currentUser">The user to check for permissions</param>
        /// <returns>The rate with the given id</returns>
        public IQueryable<Rate> Get(int id, User currentUser)
        {
            return db.Rates
                .Where(x => x.OrganizationId == currentUser.OrganizationId)
                .Where(x => x.IsDeleted == false)
                .Where(x => x.Id == id);
        }

        /// <summary>
        /// Returns a queryable collection of rates.
        /// </summary>
        /// <param name="currentUser">The user to check for permissions</param>
        /// <returns>The queryable collection of rates</returns>
        public IQueryable<Rate> GetAll(User currentUser)
        {
            return db.Rates
                .Where(x => x.OrganizationId == currentUser.OrganizationId)
                .Where(x => x.IsDeleted == false);
        }

        /// <summary>
        /// Updates the given rate with the given delta of changes.
        /// </summary>
        /// <param name="id">The id of the rate</param>
        /// <param name="patch">The changes that should be made to the rate</param>
        /// <param name="currentUser">The user to check for permissions</param>
        /// <returns>The updated rate</returns>
        public Rate Update(int id, Delta<Rate> patch, User currentUser)
        {
            var rate = db.Rates
                .Where(x => x.OrganizationId == currentUser.OrganizationId)
                .Where(x => x.Id == id)
                .FirstOrDefault();

            // Ensure that object was found
            if (rate == null) { throw new NotFoundException("No object was found with that ID in the database"); }

            // Ensure that object is not deleted
            if (rate.IsDeleted) { throw new NotFoundException("Not authorized to modify a deleted object"); }

            // Do not allow modifying some properties
            if (patch.GetChangedPropertyNames().Contains("OrganizationId") ||
                patch.GetChangedPropertyNames().Contains("ParentRateId") ||
                patch.GetChangedPropertyNames().Contains("Type") ||
                patch.GetChangedPropertyNames().Contains("IsDeleted") ||
                patch.GetChangedPropertyNames().Contains("CreatedAt"))
            {
                throw new NotAuthorizedException("Not authorized to modify CreatedAt, IsDeleted, OrganizationId, ParentRateId, Type");
            }

            // Peform the update
            patch.Patch(rate);

            db.SaveChanges();

            return rate;
        }
    }
}