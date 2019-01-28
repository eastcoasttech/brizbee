using Brizbee.Common.Models;
using Brizbee.Repositories;
using Microsoft.AspNet.OData;
using NodaTime;
using NodaTime.TimeZones;
using System;
using System.Collections.Generic;
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

        // GET: odata/Organizations/Default.TimeZones
        [HttpGet]
        [AllowAnonymous]
        public IHttpActionResult TimeZones()
        {
            List<string> zones = new List<string>();
            var now = SystemClock.Instance.GetCurrentInstant();
            var tzdb = DateTimeZoneProviders.Tzdb;
            var countryCode = "US";

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
                    DisplayValue = string.Format("({0:+HH:mm}) {1}", offset, zoneId)
                };

            //return list.ToDictionary(x => x.Id, x => x.DisplayValue);
            
            foreach (var z in list)
            {
                zones.Add(z.Id);
            }


            //foreach (TimeZoneInfo timeZone in TimeZoneInfo.GetSystemTimeZones())
            //{
            //    //zones.Add(timeZone.DisplayName.Substring(12));
            //    zones.Add(timeZone.Id);
            //}
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