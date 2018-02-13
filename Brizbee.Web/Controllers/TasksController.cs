using Brizbee.Common.Models;
using Brizbee.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Http;
using System.Web.OData;

namespace Brizbee.Controllers
{
    public class TasksController : BaseODataController
    {
        private BrizbeeWebContext db = new BrizbeeWebContext();
        private TaskRepository repo = new TaskRepository();

        // GET: odata/Tasks
        [EnableQuery(PageSize = 20, MaxExpansionDepth = 1)]
        public IQueryable<Task> GetTasks()
        {
            try
            {
                return repo.GetAll(CurrentUser());
            }
            catch (Exception)
            {
                // Return an empty result if there are errors
                return Enumerable.Empty<Task>().AsQueryable();
            }
        }

        // GET: odata/Tasks(5)
        [EnableQuery]
        public SingleResult<Task> GetTask ([FromODataUri] int key)
        {
            try
            {
                var queryable = new List<Task>() { repo.Get(key, CurrentUser()) }.AsQueryable();
                return SingleResult.Create(queryable);
            }
            catch (Exception)
            {
                // Return an empty result if there are errors
                return SingleResult.Create(Enumerable.Empty<Task>().AsQueryable());
            }
        }

        // POST: odata/Tasks
        public IHttpActionResult Post(Task task)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }
            
            task = repo.Create(task, CurrentUser());

            return Created(task);
        }

        // DELETE: odata/Tasks(5)
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