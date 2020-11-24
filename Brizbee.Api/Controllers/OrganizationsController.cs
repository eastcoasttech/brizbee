using Brizbee.Api.Serialization.DTO;
using Brizbee.Common.Models;
using Brizbee.Common.Serialization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.JsonPatch;
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
    [Route("api/[controller]")]
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
        [HttpGet("{id}")]
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
        [HttpPut("{id}")]
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
        [HttpGet]
        [Route("Countries")]
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
        [HttpGet]
        [Route("TimeZones")]
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
