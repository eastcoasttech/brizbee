using Brizbee.Common.Models;
using Brizbee.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Http;
using System.Web.OData;

namespace Brizbee.Controllers
{
    public class TasksController : BaseODataController
    {
        private BrizbeeWebContext db = new BrizbeeWebContext();
        private TaskRepository repo = new TaskRepository();

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