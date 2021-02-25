//
//  OrganizationsController.cs
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

using Brizbee.Api.Serialization.DTO;
using Brizbee.Common.Models;
using Brizbee.Common.Serialization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using NodaTime;
using NodaTime.TimeZones;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;

namespace Brizbee.Api.Controllers
{
    [ApiController]
    [Authorize]
    public class OrganizationsController : ControllerBase
    {
        private readonly IConfiguration _config;
        private readonly SqlContext _context;

        public OrganizationsController(IConfiguration configuration, SqlContext context)
        {
            _config = configuration;
            _context = context;
        }

        // GET api/Organizations/5
        [HttpGet("api/Organizations/{id}")]
        public async Task<ActionResult<OrganizationDTO>> GetOrganization(int id)
        {
            var currentUser = CurrentUser();

            // Only permit users in the same organization
            if (currentUser.OrganizationId != id)
            {
                return BadRequest();
            }

            // Prevent access to secure properties
            var organization = await _context.Organizations
                .Where(o => o.Id == id)
                .Select(o => OrganizationToDTO(o))
                .FirstOrDefaultAsync();

            if (organization == null)
            {
                return NotFound();
            }

            return organization;
        }

        // PUT api/Organizations/5
        [HttpPut("api/Organizations/{id}")]
        public IActionResult PutOrganization(int id, [FromBody] Organization patch)
        {
            var currentUser = CurrentUser();

            var organization = _context.Organizations.Find(id);

            if (organization == null)
            {
                return NotFound();
            }

            // Only permit Administrators of the same organization
            if (currentUser.Role != "Administrator" && currentUser.OrganizationId != id)
            {
                return BadRequest();
            }

            // Apply the changes
            organization.Code = patch.Code;
            organization.Name = patch.Name;
            organization.MinutesFormat = patch.MinutesFormat;

            try
            {
                _context.SaveChanges();
            }
            catch (DbUpdateConcurrencyException)
            {
                throw;
            }

            return NoContent();
        }

        // GET api/Organizations/Countries
        [HttpGet("api/Organizations/Countries")]
        public IActionResult Countries()
        {
            List<Country> countries = new List<Country>();

            CultureInfo[] cultures = CultureInfo.GetCultures(CultureTypes.SpecificCultures);

            foreach (var culture in cultures)
            {
                var region = new RegionInfo(culture.LCID);
                if (!countries.Where(c => c.Name == region.EnglishName).Any())
                {
                    countries.Add(new Country() { CountryCode = region.TwoLetterISORegionName, Name = region.EnglishName });
                }
            }

            return Ok(countries.OrderBy(c => c.Name).ToList());
        }

        // GET api/Organizations/TimeZones
        [HttpGet("api/Organizations/TimeZones")]
        public IActionResult TimeZones()
        {
            List<IanaTimeZone> zones = new List<IanaTimeZone>();
            var now = SystemClock.Instance.GetCurrentInstant();
            var tzdb = DateTimeZoneProviders.Tzdb;
            var countryCode = "";

            var list =
                from location in TzdbDateTimeZoneSource.Default.ZoneLocations
                where string.IsNullOrEmpty(countryCode) ||
                    location.CountryCode.Equals(countryCode,
                        StringComparison.OrdinalIgnoreCase)
                let zoneId = location.ZoneId
                let tz = tzdb[zoneId]
                let offset = tz.GetZoneInterval(now).StandardOffset
                orderby offset, zoneId
                select new
                {
                    Id = zoneId,
                    CountryCode = location.CountryCode
                };

            foreach (var z in list)
            {
                zones.Add(new IanaTimeZone() { Id = z.Id, CountryCode = z.CountryCode });
            }

            return Ok(zones);
        }

        private static OrganizationDTO OrganizationToDTO(Organization organization) =>
            new OrganizationDTO
            {
                Id = organization.Id,
                Name = organization.Name,
                Code = organization.Code,
                CreatedAt = organization.CreatedAt,
                MinutesFormat = organization.MinutesFormat,
                PlanId = organization.PlanId
            };

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
