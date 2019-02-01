using Brizbee.Common.Models;
using Brizbee.Common.Serialization;
using Brizbee.Repositories;
using Microsoft.AspNet.OData;
using NodaTime;
using NodaTime.TimeZones;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Web.Http;

namespace Brizbee.Controllers
{
    public class OrganizationsController : BaseODataController
    {
        private BrizbeeWebContext db = new BrizbeeWebContext();
        private OrganizationRepository repo = new OrganizationRepository();

        // GET: odata/Organizations(5)
        [EnableQuery]
        public SingleResult<Organization> GetOrganization([FromODataUri] int key)
        {
            try
            {
                var queryable = new List<Organization>() { repo.Get(key, CurrentUser()) }.AsQueryable();
                return SingleResult.Create(queryable);
            }
            catch (Exception)
            {
                // Return an empty result if there are errors
                return SingleResult.Create(Enumerable.Empty<Organization>().AsQueryable());
            }
        }

        // PATCH: odata/Organizations(5)
        [AcceptVerbs("PATCH", "MERGE")]
        public IHttpActionResult Patch([FromODataUri] int key, Delta<Organization> patch)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var organization = repo.Update(key, patch, CurrentUser());

            return Updated(organization);
        }

        // GET: odata/Organizations/Default.Countries
        [HttpGet]
        [AllowAnonymous]
        public IHttpActionResult Countries()
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

        // GET: odata/Organizations/Default.Countries
        [HttpGet]
        [AllowAnonymous]
        public IHttpActionResult TimeZones()
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
                //zones.Add(z.Id);
                zones.Add(new IanaTimeZone() { Id = z.Id, CountryCode = z.CountryCode });
            }
            
            return Ok(zones);
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