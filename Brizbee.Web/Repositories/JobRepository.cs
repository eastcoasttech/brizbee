//
//  JobRepository.cs
//  BRIZBEE API
//
//  Copyright (C) 2020 East Coast Technology Services, LLC
//
//  This file is part of the BRIZBEE API.
//
//  This program is free software: you can redistribute it and/or modify
//  it under the terms of the GNU Affero General Public License as
//  published by the Free Software Foundation, either version 3 of the
//  License, or (at your option) any later version.
//
//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//  GNU Affero General Public License for more details.
//
//  You should have received a copy of the GNU Affero General Public License
//  along with this program.  If not, see <https://www.gnu.org/licenses/>.
//

using Brizbee.Common.Models;
using Brizbee.Web.Policies;
using Microsoft.AspNet.OData;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Brizbee.Web.Repositories
{
    public class JobRepository : IDisposable
    {
        private SqlContext db = new SqlContext();

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
            job.CreatedAt = DateTime.UtcNow;
            job.CustomerId = customer.Id;

            db.Jobs.Add(job);

            db.SaveChanges();

            return job;
        }

        /// <summary>
        /// Disposes of the database connection.
        /// </summary>
        public void Dispose()
        {
            db.Dispose();
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