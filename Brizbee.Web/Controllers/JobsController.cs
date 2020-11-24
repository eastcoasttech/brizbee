//
//  JobsController.cs
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
using Brizbee.Web.Repositories;
using Microsoft.AspNet.OData;
using System.Linq;
using System.Net;
using System.Web.Http;

namespace Brizbee.Web.Controllers
{
    public class JobsController : BaseODataController
    {
        private SqlContext db = new SqlContext();
        private JobRepository repo = new JobRepository();

        // GET: odata/Jobs
        [EnableQuery(PageSize = 20)]
        public IQueryable<Job> GetJobs()
        {
            return repo.GetAll(CurrentUser());
        }

        // GET: odata/Jobs(5)
        [EnableQuery]
        public SingleResult<Job> GetJob([FromODataUri] int key)
        {
            return SingleResult.Create(repo.Get(key, CurrentUser()));
        }

        // POST: odata/Jobs
        public IHttpActionResult Post(Job job)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }
            
            job = repo.Create(job, CurrentUser());

            return Created(job);
        }

        // PATCH: odata/Jobs(5)
        [AcceptVerbs("PATCH", "MERGE")]
        public IHttpActionResult Patch([FromODataUri] int key, Delta<Job> patch)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var job = repo.Update(key, patch, CurrentUser());

            return Updated(job);
        }

        // DELETE: odata/Jobs(5)
        public IHttpActionResult Delete([FromODataUri] int key)
        {
            var currentUser = CurrentUser();

            // Only permit administrators
            if (currentUser.Role != "Administrator")
            {
                return StatusCode(HttpStatusCode.Forbidden);
            }

            // Only look within the organization's customer's jobs
            var customerIds = db.Customers
                .Where(c => c.OrganizationId == currentUser.OrganizationId)
                .Select(c => c.Id);
            var job = db.Jobs
                .Where(j => customerIds.Contains(j.CustomerId))
                .Where(j => j.Id == key)
                .FirstOrDefault();

            // Delete the object itself
            db.Jobs.Remove(job);

            db.SaveChanges();

            return StatusCode(HttpStatusCode.NoContent);
        }

        // POST: odata/Jobs/Default.NextNumber
        public IHttpActionResult NextNumber()
        {
            var organizationId = CurrentUser().OrganizationId;
            var customers = db.Customers
                .Where(c => c.OrganizationId == organizationId)
                .Select(c => c.Id);
            var max = db.Jobs
                .Where(j => customers.Contains(j.CustomerId))
                .Select(j => j.Number)
                .Max();
            if (max == null)
            {
                return Ok("1000");
            }
            else
            {
                var service = new SecurityService();
                var next = service.NxtKeyCode(max);
                return Ok(next);
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
                repo.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}