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
using Brizbee.Web.Repositories;
using Microsoft.AspNet.OData;
using System.Linq;
using System.Net;
using System.Web.Http;

namespace Brizbee.Web.Controllers
{
    public class TaskTemplatesController : BaseODataController
    {
        private SqlContext db = new SqlContext();
        private TaskTemplateRepository repo = new TaskTemplateRepository();

        // GET: odata/TaskTemplates
        [EnableQuery(PageSize = 20, MaxExpansionDepth = 1)]
        public IQueryable<TaskTemplate> GetTaskTemplates()
        {
            return repo.GetAll(CurrentUser());
        }

        // GET: odata/TaskTemplates(5)
        [EnableQuery]
        public SingleResult<TaskTemplate> GetTaskTemplate([FromODataUri] int key)
        {
            return SingleResult.Create(repo.Get(key, CurrentUser()));
        }

        // POST: odata/TaskTemplates
        public IHttpActionResult Post(TaskTemplate taskTemplate)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            taskTemplate = repo.Create(taskTemplate, CurrentUser());

            return Created(taskTemplate);
        }

        // DELETE: odata/TaskTemplates(5)
        public IHttpActionResult Delete([FromODataUri] int key)
        {
            repo.Delete(key, CurrentUser());
            return StatusCode(HttpStatusCode.NoContent);
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