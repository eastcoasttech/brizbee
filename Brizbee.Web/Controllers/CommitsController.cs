using Brizbee.Common.Models;
using Brizbee.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Http;
using System.Web.OData;

namespace Brizbee.Controllers
{
    public class CommitsController : BaseODataController
    {
        private BrizbeeWebContext db = new BrizbeeWebContext();
        private CommitRepository repo = new CommitRepository();

        // GET: odata/Commits
        [EnableQuery(PageSize = 20, MaxExpansionDepth = 1)]
        public IQueryable<Commit> GetCommits()
        {
            try
            {
                return repo.GetAll(CurrentUser());
            }
            catch (Exception)
            {
                // Return an empty result if there are errors
                return Enumerable.Empty<Commit>().AsQueryable();
            }
        }

        // GET: odata/Commits(5)
        [EnableQuery]
        public SingleResult<Commit> GetCommit([FromODataUri] int key)
        {
            try
            {
                var queryable = new List<Commit>() { repo.Get(key, CurrentUser()) }.AsQueryable();
                return SingleResult.Create(queryable);
            }
            catch (Exception)
            {
                // Return an empty result if there are errors
                return SingleResult.Create(Enumerable.Empty<Commit>().AsQueryable());
            }
        }

        // POST: odata/Commits
        public IHttpActionResult Post(Commit commit)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            commit = repo.Create(commit, CurrentUser());

            return Created(commit);
        }
        
        // POST: odata/Commits(5)/Undo
        [HttpPost]
        public IHttpActionResult Undo([FromODataUri] int key, ODataActionParameters parameters)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest();
            }

            try
            {
                repo.Undo(key, CurrentUser());
                return Ok();
            }
            catch (Exception)
            {
                return BadRequest();
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