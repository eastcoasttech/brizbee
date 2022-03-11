//
//  RatesController.cs
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

using Brizbee.Common.Exceptions;
using Brizbee.Common.Models;
using Brizbee.Web.Repositories;
using Microsoft.AspNet.OData;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Http;

namespace Brizbee.Web.Controllers
{
    public class RatesController : BaseODataController
    {
        private SqlContext db = new SqlContext();

        // GET: odata/Rates
        [EnableQuery(PageSize = 100, MaxExpansionDepth = 1)]
        public IQueryable<Rate> GetRates()
        {
            var currentUser = CurrentUser();

            return db.Rates
                .Where(x => x.OrganizationId == currentUser.OrganizationId)
                .Where(x => x.IsDeleted == false);
        }

        // GET: odata/Rates(5)
        [EnableQuery]
        public SingleResult<Rate> GetRate([FromODataUri] int key)
        {
            var currentUser = CurrentUser();

            return SingleResult.Create(db.Rates
                .Where(x => x.OrganizationId == currentUser.OrganizationId)
                .Where(x => x.IsDeleted == false)
                .Where(x => x.Id == key));
        }

        // POST: odata/Rates
        public IHttpActionResult Post(Rate rate)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var currentUser = CurrentUser();

            // Ensure that user is authorized.
            if (!currentUser.CanCreateRates)
                return StatusCode(HttpStatusCode.Forbidden);

            // Auto-generated
            rate.CreatedAt = DateTime.UtcNow;
            rate.OrganizationId = currentUser.OrganizationId;
            rate.IsDeleted = false;

            db.Rates.Add(rate);

            db.SaveChanges();

            return Created(rate);
        }

        // PATCH: odata/Rates(5)
        [AcceptVerbs("PATCH", "MERGE")]
        public IHttpActionResult Patch([FromODataUri] int key, Delta<Rate> patch)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var currentUser = CurrentUser();

            // Ensure that user is authorized.
            if (!currentUser.CanModifyRates)
                return StatusCode(HttpStatusCode.Forbidden);

            var rate = db.Rates
                .Where(x => x.OrganizationId == currentUser.OrganizationId)
                .Where(x => x.Id == key)
                .FirstOrDefault();

            // Ensure that object was found
            if (rate == null) return NotFound();

            // Ensure that object is not deleted
            if (rate.IsDeleted) return BadRequest();

            // Do not allow modifying some properties
            if (patch.GetChangedPropertyNames().Contains("OrganizationId") ||
                patch.GetChangedPropertyNames().Contains("ParentRateId") ||
                patch.GetChangedPropertyNames().Contains("Type") ||
                patch.GetChangedPropertyNames().Contains("IsDeleted") ||
                patch.GetChangedPropertyNames().Contains("CreatedAt"))
            {
                return BadRequest("Not authorized to modify CreatedAt, IsDeleted, OrganizationId, ParentRateId, Type");
            }

            // Peform the update
            patch.Patch(rate);

            db.SaveChanges();

            return Updated(rate);
        }

        // DELETE: odata/Rates(5)
        public IHttpActionResult Delete([FromODataUri] int key)
        {
            var currentUser = CurrentUser();

            var rate = db.Rates
                .Where(x => x.OrganizationId == currentUser.OrganizationId)
                .Where(x => x.IsDeleted == false)
                .Where(x => x.Id == key)
                .FirstOrDefault();

            // Ensure that user is authorized.
            if (!currentUser.CanDeleteRates)
                return StatusCode(HttpStatusCode.Forbidden);

            // Mark the object as deleted
            rate.IsDeleted = true;
            db.SaveChanges();

            return StatusCode(HttpStatusCode.NoContent);
        }

        // GET: odata/Rates/Default.BaseServiceRatesForPunches
        [HttpGet]
        [EnableQuery(PageSize = 100, MaxExpansionDepth = 1)]
        public IQueryable<Rate> BaseServiceRatesForPunches([FromODataUri] string InAt, [FromODataUri] string OutAt)
        {
            var inAt = DateTime.Parse(InAt);
            var outAt = DateTime.Parse(OutAt);
            var currentUser = CurrentUser();
            int[] userIds = db.Users
                .Where(u => u.OrganizationId == currentUser.OrganizationId)
                .Select(u => u.Id)
                .ToArray();
            var rateIds = db.Punches
                .Include("Task")
                .Where(p => userIds.Contains(p.UserId))
                .Where(p => DbFunctions.TruncateTime(p.InAt) >= inAt && DbFunctions.TruncateTime(p.OutAt) <= outAt)
                .Where(p => p.Task.BaseServiceRateId.HasValue)
                .Select(p => p.Task.BaseServiceRateId.Value);

            return db.Rates
                .Where(r => rateIds.Contains(r.Id))
                .Where(r => r.IsDeleted == false);
        }

        // GET: odata/Rates/Default.BasePayrollRatesForPunches
        [HttpGet]
        [EnableQuery(PageSize = 100, MaxExpansionDepth = 1)]
        public IQueryable<Rate> BasePayrollRatesForPunches([FromODataUri] string InAt, [FromODataUri] string OutAt)
        {
            var inAt = DateTime.Parse(InAt);
            var outAt = DateTime.Parse(OutAt);
            var currentUser = CurrentUser();
            int[] userIds = db.Users
                .Where(u => u.OrganizationId == currentUser.OrganizationId)
                .Select(u => u.Id)
                .ToArray();
            var baseRateIds = db.Punches
                .Include("Task")
                .Where(p => userIds.Contains(p.UserId))
                .Where(p => DbFunctions.TruncateTime(p.InAt) >= inAt && DbFunctions.TruncateTime(p.OutAt) <= outAt)
                .Where(p => p.Task.BasePayrollRateId.HasValue)
                .Select(p => p.Task.BasePayrollRateId.Value);

            return db.Rates
                .Where(r => baseRateIds.Contains(r.Id))
                .Where(r => r.IsDeleted == false);
        }

        // GET: odata/Rates/Default.AlternateServiceRatesForPunches
        [HttpGet]
        [EnableQuery(PageSize = 100, MaxExpansionDepth = 1)]
        public IQueryable<Rate> AlternateServiceRatesForPunches([FromODataUri] string InAt, [FromODataUri] string OutAt)
        {
            var inAt = DateTime.Parse(InAt);
            var outAt = DateTime.Parse(OutAt);
            var currentUser = CurrentUser();
            var userIds = db.Users
                .Where(u => u.OrganizationId == currentUser.OrganizationId)
                .Select(u => u.Id)
                .ToArray();
            var baseRateIds = db.Punches
                .Include("Task")
                .Where(p => userIds.Contains(p.UserId))
                .Where(p => DbFunctions.TruncateTime(p.InAt) >= inAt && DbFunctions.TruncateTime(p.OutAt) <= outAt)
                .Where(p => p.Task.BaseServiceRateId.HasValue)
                .GroupBy(p => p.Task.BaseServiceRateId)
                .Select(g => g.Key);

            return db.Rates
                .Where(r => baseRateIds.Contains(r.ParentRateId))
                .Where(r => r.IsDeleted == false);
        }

        // GET: odata/Rates/Default.AlternatePayrollRatesForPunches
        [HttpGet]
        [EnableQuery(PageSize = 100, MaxExpansionDepth = 1)]
        public IQueryable<Rate> AlternatePayrollRatesForPunches([FromODataUri] string InAt, [FromODataUri] string OutAt)
        {
            var inAt = DateTime.Parse(InAt);
            var outAt = DateTime.Parse(OutAt);
            var currentUser = CurrentUser();
            var userIds = db.Users
                .Where(u => u.OrganizationId == currentUser.OrganizationId)
                .Select(u => u.Id)
                .ToArray();
            var baseRateIds = db.Punches
                .Include("Task")
                .Where(p => userIds.Contains(p.UserId))
                .Where(p => DbFunctions.TruncateTime(p.InAt) >= inAt && DbFunctions.TruncateTime(p.OutAt) <= outAt)
                .Where(p => p.Task.BasePayrollRateId.HasValue)
                .GroupBy(p => p.Task.BasePayrollRateId)
                .Select(g => g.Key);

            return db.Rates
                .Where(r => baseRateIds.Contains(r.ParentRateId))
                .Where(r => r.IsDeleted == false);
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