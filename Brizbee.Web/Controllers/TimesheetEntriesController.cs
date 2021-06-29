//
//  TimesheetEntriesController.cs
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
using System;
using System.Collections.Generic;
using System.Data.Entity.Validation;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Http;

namespace Brizbee.Web.Controllers
{
    public class TimesheetEntriesController : BaseODataController
    {
        private SqlContext db = new SqlContext();

        // GET: odata/TimesheetEntries
        [EnableQuery(PageSize = 20, MaxExpansionDepth = 3)]
        public IQueryable<TimesheetEntry> GetTimesheetEntries()
        {
            var currentUser = CurrentUser();

            var userIds = db.Users
                .Where(u => u.OrganizationId == currentUser.OrganizationId)
                .Select(u => u.Id);

            return db.TimesheetEntries.Where(p => userIds.Contains(p.UserId));
        }

        // GET: odata/TimesheetEntries(5)
        [EnableQuery]
        public SingleResult<TimesheetEntry> GetTimesheetEntry([FromODataUri] int key)
        {
            var currentUser = CurrentUser();

            var userIds = db.Users
                .Where(u => u.OrganizationId == currentUser.OrganizationId)
                .Select(u => u.Id);

            return SingleResult.Create(db.TimesheetEntries
                .Where(p => userIds.Contains(p.UserId))
                .Where(p => p.Id == key));
        }

        // POST: odata/TimesheetEntries
        public IHttpActionResult Post(TimesheetEntry timesheetEntry)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var currentUser = CurrentUser();

            var now = DateTime.UtcNow;
            var organization = db.Organizations.Find(currentUser.OrganizationId);

            // Auto-generated
            timesheetEntry.CreatedAt = now;

            db.TimesheetEntries.Add(timesheetEntry);

            db.SaveChanges();

            return Created(timesheetEntry);
        }

        // PATCH: odata/TimesheetEntries(5)
        [AcceptVerbs("PATCH", "MERGE")]
        public IHttpActionResult Patch([FromODataUri] int key, Delta<TimesheetEntry> patch)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var currentUser = CurrentUser();

            var timesheetEntry = db.TimesheetEntries.Find(key);

            // Peform the update
            patch.Patch(timesheetEntry);

            db.SaveChanges();

            return Updated(timesheetEntry);
        }

        // DELETE: odata/TimesheetEntries(5)
        public IHttpActionResult Delete([FromODataUri] int key)
        {
            var currentUser = CurrentUser();

            var timesheetEntry = db.TimesheetEntries
                .Include("User")
                .Where(t => t.Id == key)
                .FirstOrDefault();

            // Ensure that user is authorized.
            if (currentUser.Role != "Administrator" ||
                currentUser.OrganizationId != timesheetEntry.User.OrganizationId)
                return BadRequest();

            // Delete the object itself
            db.TimesheetEntries.Remove(timesheetEntry);

            db.SaveChanges();

            return StatusCode(HttpStatusCode.NoContent);
        }

        // POST: odata/TimesheetEntries/Default.Add
        [HttpPost]
        public IHttpActionResult Add(ODataActionParameters parameters)
        {
            var taskId = (int)parameters["TaskId"];
            var enteredAt = DateTime.Parse(parameters["EnteredAt"] as string);
            var minutes = (int)parameters["Minutes"];
            var notes = (string)parameters["Notes"];

            var currentUser = CurrentUser();

            try
            {
                var timesheetEntry = db.TimesheetEntries.Add(new TimesheetEntry()
                {
                    CreatedAt = DateTime.UtcNow,
                    EnteredAt = enteredAt,
                    Minutes = minutes,
                    Notes = notes,
                    TaskId = taskId,
                    UserId = currentUser.Id
                });
                db.SaveChanges();

                return Created(timesheetEntry);
            }
            catch (DbEntityValidationException e)
            {
                string message = "";

                foreach (var eve in e.EntityValidationErrors)
                {
                    foreach (var ve in eve.ValidationErrors)
                    {
                        message += string.Format("{0} has error '{1}'; ", ve.PropertyName, ve.ErrorMessage);
                    }
                }

                return Content(HttpStatusCode.BadRequest, message);
            }
            catch (Exception ex)
            {
                return Content(HttpStatusCode.BadRequest, ex.Message);
            }
        }
    }
}