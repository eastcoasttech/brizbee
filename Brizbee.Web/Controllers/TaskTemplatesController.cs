using Brizbee.Common.Database;
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