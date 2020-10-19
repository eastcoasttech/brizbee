using Brizbee.Common.Models;
using Brizbee.Web.Repositories;
using Microsoft.AspNet.OData;
using System;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web.Http;

namespace Brizbee.Web.Controllers
{
    public class TasksController : BaseODataController
    {
        private SqlContext db = new SqlContext();
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

        // GET: odata/Tasks/Default.Search
        [HttpGet]
        public IHttpActionResult Search(string number)
        {
            var currentUser = CurrentUser();

            // Search only tasks that belong to customers in the organization
            var task = db.Tasks
                .Include("Job")
                .Include("Customer")
                .Where(t => t.Job.Customer.OrganizationId == currentUser.OrganizationId)
                .Where(t => t.Number == number)
                .FirstOrDefault();

            if (task == null)
            {
                return NotFound();
            }

            return Ok(task);
        }

        // GET: odata/Tasks/Default.ForPunches
        [HttpGet]
        [EnableQuery(PageSize = 30, MaxExpansionDepth = 1)]
        public IQueryable<Task> ForPunches(string InAt, string OutAt)
        {
            var inAt = DateTime.Parse(InAt);
            var outAt = DateTime.Parse(OutAt);
            var currentUser = CurrentUser();
            int[] userIds = db.Users
                .Where(u => u.OrganizationId == currentUser.OrganizationId)
                .Select(u => u.Id)
                .ToArray();
            var taskIds = db.Punches
                .Where(p => userIds.Contains(p.UserId))
                .Where(p => DbFunctions.TruncateTime(p.InAt) >= inAt && DbFunctions.TruncateTime(p.OutAt) <= outAt)
                .GroupBy(p => p.TaskId)
                .Select(g => g.Key);

            return db.Tasks.Where(t => taskIds.Contains(t.Id));
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