using Brizbee.Common.Models;
using Brizbee.Repositories;
using Microsoft.AspNet.OData;
using System.Linq;
using System.Net;
using System.Web.Http;

namespace Brizbee.Controllers
{
    public class TasksController : BaseODataController
    {
        private BrizbeeWebContext db = new BrizbeeWebContext();
        private TaskRepository repo = new TaskRepository();

        // GET: odata/Tasks
        [EnableQuery(PageSize = 20, MaxExpansionDepth = 3)]
        public IQueryable<Task> GetTasks()
        {
            return repo.GetAll(CurrentUser());
        }

        // GET: odata/Tasks(5)
        [EnableQuery(MaxExpansionDepth = 3)]
        public SingleResult<Task> GetTask ([FromODataUri] int key)
        {
            return SingleResult.Create(repo.Get(key, CurrentUser()));
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

        // PATCH: odata/Tasks(5)
        [AcceptVerbs("PATCH", "MERGE")]
        public IHttpActionResult Patch([FromODataUri] int key, Delta<Task> patch)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var task = repo.Update(key, patch, CurrentUser());

            return Updated(task);
        }

        // DELETE: odata/Tasks(5)
        public IHttpActionResult Delete([FromODataUri] int key)
        {
            repo.Delete(key, CurrentUser());
            return StatusCode(HttpStatusCode.NoContent);
        }

        // POST: odata/Tasks/Default.NextNumber
        public IHttpActionResult NextNumber()
        {
            var organizationId = CurrentUser().OrganizationId;
            var customers = db.Customers
                .Where(c => c.OrganizationId == organizationId)
                .Select(c => c.Id);
            var jobs = db.Jobs
                .Where(j => customers.Contains(j.CustomerId))
                .Select(j => j.Id);
            var max = db.Tasks
                .Where(t => jobs.Contains(t.JobId))
                .Select(t => t.Number)
                .Max();
            var service = new SecurityService();
            var next = service.NxtKeyCode(max);
            return Ok(next);
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