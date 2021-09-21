//
//  TaskTemplatesController.cs
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
using Microsoft.AspNet.OData;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Web.Http;

namespace Brizbee.Web.Controllers
{
    public class TaskTemplatesController : BaseODataController
    {
        private SqlContext db = new SqlContext();

        // GET: odata/TaskTemplates
        [EnableQuery(PageSize = 20, MaxExpansionDepth = 1)]
        public IQueryable<TaskTemplate> GetTaskTemplates()
        {
            var currentUser = CurrentUser();

            // Ensure that user is authorized.
            if (!currentUser.CanCreateProjects)
                return new List<TaskTemplate>().AsQueryable();

            return db.TaskTemplates
                .Where(t => t.OrganizationId == currentUser.OrganizationId);
        }

        // GET: odata/TaskTemplates(5)
        [EnableQuery]
        public SingleResult<TaskTemplate> GetTaskTemplate([FromODataUri] int key)
        {
            var currentUser = CurrentUser();

            // Ensure that user is authorized.
            if (!currentUser.CanCreateProjects)
                return SingleResult.Create(new List<TaskTemplate>().AsQueryable());

            return SingleResult.Create(db.TaskTemplates
                .Where(t => t.OrganizationId == currentUser.OrganizationId)
                .Where(t => t.Id == key));
        }

        // POST: odata/TaskTemplates
        public IHttpActionResult Post(TaskTemplate taskTemplate)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var currentUser = CurrentUser();

            // Ensure that user is authorized.
            if (!currentUser.CanCreateProjects)
                return BadRequest();

            // Auto-generated.
            taskTemplate.CreatedAt = DateTime.UtcNow;
            taskTemplate.OrganizationId = currentUser.OrganizationId;

            db.TaskTemplates.Add(taskTemplate);

            db.SaveChanges();

            return Created(taskTemplate);
        }

        // DELETE: odata/TaskTemplates(5)
        public IHttpActionResult Delete([FromODataUri] int key)
        {
            var currentUser = CurrentUser();

            var taskTemplate = db.TaskTemplates.Find(key);

            // Ensure that user is authorized.
            if (!currentUser.CanCreateProjects ||
                taskTemplate.OrganizationId != currentUser.OrganizationId)
                return BadRequest();

            // Delete the object itself.
            db.TaskTemplates.Remove(taskTemplate);

            db.SaveChanges();

            return StatusCode(HttpStatusCode.NoContent);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
