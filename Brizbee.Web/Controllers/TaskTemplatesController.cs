using Brizbee.Common.Models;
using Brizbee.Repositories;
using Microsoft.AspNet.OData;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Web.Http;

namespace Brizbee.Controllers
{
    public class TaskTemplatesController : BaseODataController
    {
        private BrizbeeWebContext db = new BrizbeeWebContext();
        private TaskTemplateRepository repo = new TaskTemplateRepository();

        // GET: odata/TaskTemplates
        [EnableQuery(PageSize = 20, MaxExpansionDepth = 1)]
        public IQueryable<TaskTemplate> GetTaskTemplates()
        {
            try
            {
                return repo.GetAll(CurrentUser());
            }
            catch (Exception)
            {
                // Return an empty result if there are errors
                return Enumerable.Empty<TaskTemplate>().AsQueryable();
            }
        }

        // GET: odata/TaskTemplates(5)
        [EnableQuery]
        public SingleResult<TaskTemplate> GetTaskTemplate([FromODataUri] int key)
        {
            try
            {
                var queryable = new List<TaskTemplate>() { repo.Get(key, CurrentUser()) }.AsQueryable();
                return SingleResult.Create(queryable);
            }
            catch (Exception)
            {
                // Return an empty result if there are errors
                return SingleResult.Create(Enumerable.Empty<TaskTemplate>().AsQueryable());
            }
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