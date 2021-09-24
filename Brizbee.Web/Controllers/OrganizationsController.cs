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

using Azure.Storage.Blobs;
using Brizbee.Common.Models;
using Brizbee.Common.Serialization;
using Brizbee.Common.Serialization.Alerts;
using Brizbee.Web.Repositories;
using Microsoft.AspNet.OData;
using Newtonsoft.Json;
using NodaTime;
using NodaTime.TimeZones;
using Stripe;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Web.Http;

namespace Brizbee.Web.Controllers
{
    public class OrganizationsController : BaseODataController
    {
        private SqlContext db = new SqlContext();

        // GET: odata/Organizations(5)
        [EnableQuery]
        public SingleResult<Organization> GetOrganization([FromODataUri] int key)
        {
            var currentUser = CurrentUser();

            return SingleResult.Create(db.Organizations
                .Where(o => o.Id == currentUser.OrganizationId)
                .Where(o => o.Id == key));
        }

        // PATCH: odata/Organizations(5)
        [AcceptVerbs("PATCH", "MERGE")]
        public IHttpActionResult Patch([FromODataUri] int key, Delta<Organization> patch)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var currentUser = CurrentUser();

            var organization = db.Organizations.Find(key);

            // Ensure that object was found
            if (organization == null) return NotFound();

            // Ensure that user is authorized
            if (!currentUser.CanModifyOrganizationDetails ||
                currentUser.OrganizationId != key)
                return BadRequest();

            // Peform the update
            patch.Patch(organization);
            
            // Update the Stripe payment source if it is provided
            if (patch.GetChangedPropertyNames().Contains("StripeSourceId"))
            {
                var customerService = new CustomerService();
                var sourceService = new SourceService();
                
                // Attach the card source id to the customer
                var attachOptions = new SourceAttachOptions()
                {
                    Source = organization.StripeSourceId
                };
                sourceService.Attach(organization.StripeCustomerId, attachOptions);

                // Update the customer's default source
                var customerOptions = new CustomerUpdateOptions()
                {
                    DefaultSource = organization.StripeSourceId
                };
                Stripe.Customer customer = customerService.Update(organization.StripeCustomerId, customerOptions);

                var source = sourceService.Get(organization.StripeSourceId);

                // Record the card details
                organization.StripeSourceCardLast4 = source.Card.Last4;
                organization.StripeSourceCardBrand = source.Card.Brand;
                organization.StripeSourceCardExpirationMonth = source.Card.ExpMonth.ToString();
                organization.StripeSourceCardExpirationYear = source.Card.ExpYear.ToString();
            }

            db.SaveChanges();

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

        // GET: odata/Organizations/Default.TimeZones
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
            }
            base.Dispose(disposing);
        }
    }
}