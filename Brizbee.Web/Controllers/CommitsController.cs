//
//  CommitsController.cs
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
using Brizbee.Web.Serialization;
using Brizbee.Web.Services;
using Microsoft.AspNet.OData;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;

namespace Brizbee.Web.Controllers
{
    public class CommitsController : BaseODataController
    {
        private SqlContext db = new SqlContext();
        private CommitRepository repo = new CommitRepository();

        // GET: odata/Commits
        [EnableQuery(PageSize = 20, MaxExpansionDepth = 1)]
        public IQueryable<Commit> GetCommits()
        {
            return repo.GetAll(CurrentUser());
        }

        // GET: odata/Commits(5)
        [EnableQuery]
        public SingleResult<Commit> GetCommit([FromODataUri] int key)
        {
            return SingleResult.Create(repo.Get(key, CurrentUser()));
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

            repo.Undo(key, CurrentUser());
            return Ok();
        }

        // PATCH: odata/Commits(5)
        [AcceptVerbs("PATCH", "MERGE")]
        public IHttpActionResult Patch([FromODataUri] int key, Delta<Commit> patch)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var commit = repo.Update(key, patch, CurrentUser());

            return Updated(commit);
        }

        // GET: odata/Commits(5)/Default.Export
        [HttpGet]
        public HttpResponseMessage Export([FromODataUri] int key, [FromODataUri] string Delimiter)
        {
            var commitId = key;
            var currentUser = CurrentUser();

            var exportService = new ExportService(commitId, currentUser.Id);
            string csv = exportService.BuildCsv(Delimiter);

            var response = new HttpResponseMessage(HttpStatusCode.OK);
            response.Content = new StringContent(csv, System.Text.Encoding.UTF8, "text/plain");
            return response;
        }

        /// <summary>
        /// Disposes of the resources used during each request (instance)
        /// of this controller.
        /// </summary>
        /// <param name="disposing">Whether or not the resources should be disposed</param>
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