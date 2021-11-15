//
//  OrganizationsController.cs
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
using Brizbee.Core.Serialization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData.Deltas;
using Microsoft.AspNetCore.OData.Formatter;
using Microsoft.AspNetCore.OData.Query;
using Microsoft.AspNetCore.OData.Results;
using Microsoft.AspNetCore.OData.Routing.Controllers;
using NodaTime;
using NodaTime.TimeZones;
using Stripe;
using System.Globalization;

namespace Brizbee.Api.Controllers
{
    public class OrganizationsController : ODataController
    {
        private readonly IConfiguration _configuration;
        private readonly SqlContext _context;

        public OrganizationsController(IConfiguration configuration, SqlContext context)
        {
            _configuration = configuration;
            _context = context;
        }

        // GET: odata/Organizations(5)
        [EnableQuery]
        public SingleResult<Organization> GetOrganization([FromODataUri] int key)
        {
            var currentUser = CurrentUser();

            return SingleResult.Create(_context.Organizations
                .Where(o => o.Id == currentUser.OrganizationId)
                .Where(o => o.Id == key));
        }

        // PATCH: odata/Organizations(5)
        public IActionResult Patch([FromODataUri] int key, Delta<Organization> patch)
        {
            var currentUser = CurrentUser();

            var organization = _context.Organizations.Find(key);

            // Ensure that object was found
            if (organization == null) return NotFound();

            // Ensure that user is authorized
            if (!currentUser.CanModifyOrganizationDetails ||
                currentUser.OrganizationId != key)
                return BadRequest();

            // Do not allow modifying some properties.
            if (patch.GetChangedPropertyNames().Contains("StripeCustomerId") ||
                patch.GetChangedPropertyNames().Contains("StripeSourceCardBrand") ||
                patch.GetChangedPropertyNames().Contains("StripeSourceCardLast4") ||
                patch.GetChangedPropertyNames().Contains("StripeSourceCardExpirationMonth") ||
                patch.GetChangedPropertyNames().Contains("StripeSourceCardExpirationYear") ||
                patch.GetChangedPropertyNames().Contains("StripeSubscriptionId") ||
                patch.GetChangedPropertyNames().Contains("CreatedAt") ||
                patch.GetChangedPropertyNames().Contains("Id"))
            {
                return BadRequest("Not authorized to modify CreatedAt, Id, StripeCustomerId, StripeSourceCardBrand, StripeSourceCardLast4, StripeSourceCardExpirationMonth, StripeSourceCardExpirationYear, or StripeSubscriptionId.");
            }

            // Peform the update
            patch.Patch(organization);

            // Validate the model.
            ModelState.ClearValidationState(nameof(organization));
            if (!TryValidateModel(organization, nameof(organization)))
                return BadRequest();

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

            _context.SaveChanges();

            return NoContent();
        }

        // GET: odata/Organizations/Default.Countries
        [HttpGet]
        [AllowAnonymous]
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

        // GET: odata/Organizations/Default.TimeZones
        [HttpGet]
        [AllowAnonymous]
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