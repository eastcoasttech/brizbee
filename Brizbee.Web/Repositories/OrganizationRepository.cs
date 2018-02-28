﻿using Brizbee.Common.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.OData;

namespace Brizbee.Repositories
{
    public class OrganizationRepository : IDisposable
    {
        private BrizbeeWebContext db = new BrizbeeWebContext();

        /// <summary>
        /// Disposes of the database connection.
        /// </summary>
        public void Dispose()
        {
            db.Dispose();
        }

        /// <summary>
        /// Updates the given organization with the given delta of changes.
        /// </summary>
        /// <param name="id">The id of the organization</param>
        /// <param name="patch">The changes that should be made to the organization</param>
        /// <param name="currentUser">The user to check for permissions</param>
        /// <returns>The updated organization</returns>
        public Organization Update(int id, Delta<Organization> patch, User currentUser)
        {
            var organization = db.Organizations.Find(id);

            // Ensure that object was found
            if (organization == null) { throw new Exception("No object was found with that ID in the database"); }

            // Ensure that user is authorized
            //if (!TaskPolicy.CanUpdate(task, currentUser))
            //{
            //    throw new Exception("Not authorized to modify the object");
            //}

            // Peform the update
            patch.Patch(organization);

            db.SaveChanges();

            return organization;
        }
    }
}