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
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;

namespace Brizbee.Web.Controllers
{
    public class CommitsController : BaseODataController
    {
        private SqlContext db = new SqlContext();
        
        // GET: odata/Commits
        [EnableQuery(PageSize = 20, MaxExpansionDepth = 1)]
        public IQueryable<Commit> GetCommits()
        {
            var currentUser = CurrentUser();

            return db.Commits
                .Where(c => c.OrganizationId == currentUser.OrganizationId);
        }

        // GET: odata/Commits(5)
        [EnableQuery]
        public SingleResult<Commit> GetCommit([FromODataUri] int key)
        {
            var currentUser = CurrentUser();

            return SingleResult.Create(db.Commits
                .Where(c => c.OrganizationId == currentUser.OrganizationId)
                .Where(c => c.Id == key));
        }

        // POST: odata/Commits
        public IHttpActionResult Post(Commit commit)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var currentUser = CurrentUser();

            if (currentUser.Role != "Administrator")
                return BadRequest();

            using (var transaction = db.Database.BeginTransaction())
            {
                try
                {
                    var inAt = new DateTime(commit.InAt.Year, commit.InAt.Month, commit.InAt.Day, 0, 0, 0, DateTimeKind.Unspecified);
                    var outAt = new DateTime(commit.OutAt.Year, commit.OutAt.Month, commit.OutAt.Day, 23, 59, 59, DateTimeKind.Unspecified);

                    // Ensure that no two commits overlap
                    var overlap = db.Commits
                        .Where(c => c.OrganizationId == commit.OrganizationId)
                        .Where(c => (inAt < c.OutAt) && (c.InAt < outAt))
                        .FirstOrDefault();
                    if (overlap != null)
                    {
                        return BadRequest(string.Format(
                                "The commit overlaps another commit: {0} thru {1}",
                                overlap.InAt.ToString("yyyy-MM-dd"),
                                overlap.OutAt.ToString("yyyy-MM-dd")
                            ));
                    }

                    // Auto-generated
                    commit.CreatedAt = DateTime.UtcNow;
                    commit.Guid = Guid.NewGuid();
                    commit.OrganizationId = currentUser.OrganizationId;
                    commit.UserId = currentUser.Id;
                    commit.InAt = inAt;
                    commit.OutAt = outAt;

                    db.Commits.Add(commit);

                    db.SaveChanges();

                    // Split the punches at midnight.
                    var service = new PunchService();
                    int[] userIds = db.Users
                        .Where(u => u.OrganizationId == currentUser.OrganizationId)
                        .Select(u => u.Id)
                        .ToArray();
                    var originalPunchesTracked = db.Punches
                        .Where(p => userIds.Contains(p.UserId))
                        .Where(p => p.InAt >= inAt && p.OutAt.HasValue && p.OutAt.Value <= outAt)
                        .Where(p => !p.CommitId.HasValue); // Only uncommited punches
                    var originalPunchesNotTracked = originalPunchesTracked
                        .AsNoTracking() // Will be manipulated in memory
                        .ToList();
                    var splitPunches = service.SplitAtMidnight(originalPunchesNotTracked, currentUser);

                    // Delete the old punches and save the new ones.
                    db.Punches.RemoveRange(originalPunchesTracked);
                    db.SaveChanges();

                    // Save the commit id with the new punches.
                    foreach (var punch in splitPunches)
                    {
                        punch.CommitId = commit.Id;
                    }
                    commit.PunchCount = splitPunches.Count();

                    db.Punches.AddRange(splitPunches);
                    db.SaveChanges();

                    transaction.Commit();

                    return Created(commit);
                }
                catch (Exception ex)
                {
                    transaction.Rollback();

                    return BadRequest(ex.ToString());
                }
            }
        }
        
        // POST: odata/Commits(5)/Undo
        [HttpPost]
        public IHttpActionResult Undo([FromODataUri] int key, ODataActionParameters parameters)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest();
            }

            var currentUser = CurrentUser();

            var commit = db.Commits
                .Where(c => c.OrganizationId == currentUser.OrganizationId)
                .FirstOrDefault(c => c.Id == key);

            if (commit != null)
            {
                var punches = db.Punches.Where(p => p.CommitId == commit.Id).ToList();
                punches.ForEach(p => {
                    p.CommitId = null;
                });

                db.Commits.Remove(commit);

                var qboExports = db.QuickBooksOnlineExports.Where(x => x.CommitId == key).ToList();
                qboExports.ForEach(x => {
                    db.QuickBooksOnlineExports.Remove(x);
                });

                var qbdExports = db.QuickBooksDesktopExports.Where(x => x.CommitId == key).ToList();
                qbdExports.ForEach(x => {
                    db.QuickBooksDesktopExports.Remove(x);
                });

                db.SaveChanges();

                return Ok();
            }
            else
            {
                return NotFound();
            }
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
            }
            base.Dispose(disposing);
        }
    }
}