//
//  RatesExpandedController.cs
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

using System.Net;
using Brizbee.Core.Models;
using Microsoft.AspNetCore.Mvc;

namespace Brizbee.Api.Controllers;

public class RatesExpandedController : ControllerBase
{
    private readonly SqlContext _context;

    public RatesExpandedController(SqlContext context)
    {
        _context = context;
    }

    // GET: api/RatesExpanded/AlternateServiceRatesForPunches
    [HttpGet("api/RatesExpanded/AlternateServiceRatesForPunches")]
    public IActionResult GetAlternateServiceRatesForPunches([FromQuery] DateTime inAt, [FromQuery] DateTime outAt,
        [FromQuery] int skip = 0, [FromQuery] int pageSize = 1000)
    {
        if (pageSize > 1000) { return BadRequest(); }

        var currentUser = CurrentUser();
        var userIds = _context.Users
            .Where(u => u.OrganizationId == currentUser.OrganizationId)
            .Where(u => u.IsDeleted == false)
            .Select(u => u.Id);
        var baseRateIds = _context.Punches
            .Where(p => userIds.Contains(p.UserId))
            .Where(p => p.InAt.Date >= inAt && p.OutAt.Value.Date <= outAt)
            .Where(p => p.Task.BaseServiceRateId.HasValue)
            .GroupBy(p => p.Task.BaseServiceRateId)
            .Select(g => g.Key);

        var rates = _context.Rates
            .Where(r => baseRateIds.Contains(r.ParentRateId))
            .Where(r => r.IsDeleted == false);

        return new JsonResult(rates)
        {
            StatusCode = (int)HttpStatusCode.OK
        };
    }

    // GET: api/RatesExpanded/AlternatePayrollRatesForPunches
    [HttpGet("api/RatesExpanded/AlternatePayrollRatesForPunches")]
    public IActionResult GetAlternatePayrollRatesForPunches([FromQuery] DateTime inAt, [FromQuery] DateTime outAt,
        [FromQuery] int skip = 0, [FromQuery] int pageSize = 1000)
    {
        if (pageSize > 1000) { return BadRequest(); }

        var currentUser = CurrentUser();
        var userIds = _context.Users
            .Where(u => u.OrganizationId == currentUser.OrganizationId)
            .Where(u => u.IsDeleted == false)
            .Select(u => u.Id);
        var baseRateIds = _context.Punches
            .Where(p => userIds.Contains(p.UserId))
            .Where(p => p.InAt.Date >= inAt && p.OutAt.Value.Date <= outAt)
            .Where(p => p.Task.BasePayrollRateId.HasValue)
            .GroupBy(p => p.Task.BasePayrollRateId)
            .Select(g => g.Key);

        var rates = _context.Rates
            .Where(r => baseRateIds.Contains(r.ParentRateId))
            .Where(r => r.IsDeleted == false);

        return new JsonResult(rates)
        {
            StatusCode = (int)HttpStatusCode.OK
        };
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
