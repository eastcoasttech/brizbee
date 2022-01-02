//
//  RatesController.cs
//  BRIZBEE API
//
//  Copyright (C) 2019-2021 East Coast Technology Services, LLC
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

using Brizbee.Core.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData.Deltas;
using Microsoft.AspNetCore.OData.Formatter;
using Microsoft.AspNetCore.OData.Query;
using Microsoft.AspNetCore.OData.Results;
using Microsoft.AspNetCore.OData.Routing.Controllers;
using Microsoft.EntityFrameworkCore;

namespace Brizbee.Api.Controllers
{
    public class RatesController : ODataController
    {
        private readonly IConfiguration _configuration;
        private readonly SqlContext _context;

        public RatesController(IConfiguration configuration, SqlContext context)
        {
            _configuration = configuration;
            _context = context;
        }

        // GET: odata/Rates
        [EnableQuery(PageSize = 100, MaxExpansionDepth = 1)]
        public IQueryable<Rate> GetRates()
        {
            var currentUser = CurrentUser();

            return _context.Rates
                .Where(x => x.OrganizationId == currentUser.OrganizationId)
                .Where(x => x.IsDeleted == false);
        }

        // GET: odata/Rates(5)
        [EnableQuery]
        public SingleResult<Rate> GetRate([FromODataUri] int key)
        {
            var currentUser = CurrentUser();

            return SingleResult.Create(_context.Rates
                .Where(x => x.OrganizationId == currentUser.OrganizationId)
                .Where(x => x.IsDeleted == false)
                .Where(x => x.Id == key));
        }

        // POST: odata/Rates
        public IActionResult Post([FromBody] Rate rate)
        {
            var currentUser = CurrentUser();

            // Ensure that user is authorized.
            if (!currentUser.CanCreateRates)
                return Forbid();

            // Auto-generated.
            rate.CreatedAt = DateTime.UtcNow;
            rate.OrganizationId = currentUser.OrganizationId;
            rate.IsDeleted = false;

            // Validate the model.
            ModelState.ClearValidationState(nameof(rate));
            if (!TryValidateModel(rate, nameof(rate)))
            {
                var errors = new List<string>();

                foreach (var modelStateKey in ModelState.Keys)
                {
                    var value = ModelState[modelStateKey];

                    if (value == null) continue;

                    foreach (var error in value.Errors)
                    {
                        errors.Add(error.ErrorMessage);
                    }
                }

                var message = string.Join(", ", errors);
                return BadRequest(message);
            }

            _context.Rates.Add(rate);

            _context.SaveChanges();

            return Ok(rate);
        }

        // PATCH: odata/Rates(5)
        public IActionResult Patch([FromODataUri] int key, Delta<Rate> patch)
        {
            var currentUser = CurrentUser();

            var rate = _context.Rates
                .Where(x => x.OrganizationId == currentUser.OrganizationId)
                .Where(x => x.Id == key)
                .FirstOrDefault();

            // Ensure that object was found.
            if (rate == null) return NotFound();

            // Ensure that user is authorized.
            if (!currentUser.CanModifyRates ||
                currentUser.OrganizationId != rate.OrganizationId)
                return Forbid();

            // Ensure that object is not deleted.
            if (rate.IsDeleted) return BadRequest();

            // Do not allow modifying some properties.
            if (patch.GetChangedPropertyNames().Contains("OrganizationId") ||
                patch.GetChangedPropertyNames().Contains("ParentRateId") ||
                patch.GetChangedPropertyNames().Contains("Type") ||
                patch.GetChangedPropertyNames().Contains("IsDeleted") ||
                patch.GetChangedPropertyNames().Contains("Id") ||
                patch.GetChangedPropertyNames().Contains("CreatedAt"))
            {
                return BadRequest("Not authorized to modify CreatedAt, Id, IsDeleted, OrganizationId, ParentRateId, Type");
            }

            // Peform the update.
            patch.Patch(rate);

            // Validate the model.
            ModelState.ClearValidationState(nameof(rate));
            if (!TryValidateModel(rate, nameof(rate)))
                return BadRequest();

            _context.SaveChanges();

            return NoContent();
        }

        // DELETE: odata/Rates(5)
        public IActionResult Delete([FromODataUri] int key)
        {
            var currentUser = CurrentUser();

            var rate = _context.Rates
                .Where(x => x.OrganizationId == currentUser.OrganizationId)
                .Where(x => x.IsDeleted == false)
                .Where(x => x.Id == key)
                .FirstOrDefault();

            // Ensure that object was found.
            if (rate == null) return NotFound();

            // Ensure that user is authorized.
            if (!currentUser.CanDeleteRates)
                return Forbid();

            // Mark the object as deleted
            rate.IsDeleted = true;

            _context.SaveChanges();

            return NoContent();
        }

        // GET: odata/Rates/BaseServiceRatesForPunches
        [HttpGet]
        [EnableQuery(PageSize = 30, MaxExpansionDepth = 1)]
        public IQueryable<Rate> BaseServiceRatesForPunches([FromODataUri] DateTimeOffset InAt, [FromODataUri] DateTimeOffset OutAt)
        {
            var currentUser = CurrentUser();
            int[] userIds = _context.Users
                .Where(u => u.OrganizationId == currentUser.OrganizationId)
                .Select(u => u.Id)
                .ToArray();
            var rateIds = _context.Punches
                .Include("Task")
                .Where(p => userIds.Contains(p.UserId))
                .Where(p => p.InAt.Date >= InAt && p.OutAt.Value.Date <= OutAt)
                .Where(p => p.Task.BaseServiceRateId.HasValue)
                .Select(p => p.Task.BaseServiceRateId.Value);

            return _context.Rates
                .Where(r => rateIds.Contains(r.Id))
                .Where(r => r.IsDeleted == false);
        }

        // GET: odata/Rates/BasePayrollRatesForPunches
        [HttpGet]
        [EnableQuery(PageSize = 30, MaxExpansionDepth = 1)]
        public IQueryable<Rate> BasePayrollRatesForPunches([FromODataUri] DateTimeOffset InAt, [FromODataUri] DateTimeOffset OutAt)
        {
            var currentUser = CurrentUser();
            int[] userIds = _context.Users
                .Where(u => u.OrganizationId == currentUser.OrganizationId)
                .Select(u => u.Id)
                .ToArray();
            var baseRateIds = _context.Punches
                .Include("Task")
                .Where(p => userIds.Contains(p.UserId))
                .Where(p => p.InAt.Date >= InAt && p.OutAt.Value.Date <= OutAt)
                .Where(p => p.Task.BasePayrollRateId.HasValue)
                .Select(p => p.Task.BasePayrollRateId.Value);

            return _context.Rates
                .Where(r => baseRateIds.Contains(r.Id))
                .Where(r => r.IsDeleted == false);
        }

        // GET: odata/Rates/AlternateServiceRatesForPunches
        [HttpGet]
        [EnableQuery(PageSize = 30, MaxExpansionDepth = 1)]
        public IQueryable<Rate> AlternateServiceRatesForPunches([FromODataUri] DateTimeOffset InAt, [FromODataUri] DateTimeOffset OutAt)
        {
            var currentUser = CurrentUser();
            var userIds = _context.Users
                .Where(u => u.OrganizationId == currentUser.OrganizationId)
                .Select(u => u.Id)
                .ToArray();
            var baseRateIds = _context.Punches
                .Include("Task")
                .Where(p => userIds.Contains(p.UserId))
                .Where(p => p.InAt.Date >= InAt && p.OutAt.Value.Date <= OutAt)
                .Where(p => p.Task.BaseServiceRateId.HasValue)
                .GroupBy(p => p.Task.BaseServiceRateId)
                .Select(g => g.Key);

            return _context.Rates
                .Where(r => baseRateIds.Contains(r.ParentRateId))
                .Where(r => r.IsDeleted == false);
        }

        // GET: odata/Rates/AlternatePayrollRatesForPunches
        [HttpGet]
        [EnableQuery(PageSize = 30, MaxExpansionDepth = 1)]
        public IQueryable<Rate> AlternatePayrollRatesForPunches([FromODataUri] DateTimeOffset InAt, [FromODataUri] DateTimeOffset OutAt)
        {
            var currentUser = CurrentUser();
            var userIds = _context.Users
                .Where(u => u.OrganizationId == currentUser.OrganizationId)
                .Select(u => u.Id)
                .ToArray();
            var baseRateIds = _context.Punches
                .Include("Task")
                .Where(p => userIds.Contains(p.UserId))
                .Where(p => p.InAt.Date >= InAt && p.OutAt.Value.Date <= OutAt)
                .Where(p => p.Task.BasePayrollRateId.HasValue)
                .GroupBy(p => p.Task.BasePayrollRateId)
                .Select(g => g.Key);

            return _context.Rates
                .Where(r => baseRateIds.Contains(r.ParentRateId))
                .Where(r => r.IsDeleted == false);
        }

        private User CurrentUser()
        {
            var type = "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier";
            var sub = HttpContext.User.Claims.FirstOrDefault(c => c.Type == type).Value;
            var currentUserId = int.Parse(sub);
            return _context.Users
                .Where(u => u.Id == currentUserId)
                .FirstOrDefault();
        }
    }
}
