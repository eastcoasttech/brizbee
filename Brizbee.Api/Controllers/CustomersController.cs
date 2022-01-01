//
//  CustomersController.cs
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

using Brizbee.Api.Services;
using Brizbee.Core.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData.Deltas;
using Microsoft.AspNetCore.OData.Formatter;
using Microsoft.AspNetCore.OData.Query;
using Microsoft.AspNetCore.OData.Results;
using Microsoft.AspNetCore.OData.Routing.Controllers;

namespace Brizbee.Api.Controllers
{
    public class CustomersController : ODataController
    {
        private readonly IConfiguration _configuration;
        private readonly SqlContext _context;

        public CustomersController(IConfiguration configuration, SqlContext context)
        {
            _configuration = configuration;
            _context = context;
        }

        // GET: odata/Customers
        [HttpGet]
        [EnableQuery(PageSize = 1000)]
        public IQueryable<Customer> GetCustomers()
        {
            var currentUser = CurrentUser();

            return _context.Customers
                .Where(c => c.OrganizationId == currentUser.OrganizationId);
        }

        // GET: odata/Customers(5)
        [HttpGet]
        [EnableQuery]
        public SingleResult<Customer> GetCustomer([FromODataUri] int key)
        {
            var currentUser = CurrentUser();

            return SingleResult.Create(_context.Customers
                .Where(c => c.OrganizationId == currentUser.OrganizationId)
                .Where(c => c.Id == key));
        }

        // POST: odata/Customers
        public IActionResult Post([FromBody] Customer customer)
        {
            var currentUser = CurrentUser();

            // Ensure that user is authorized.
            if (!currentUser.CanCreateCustomers)
                return Forbid();

            // Auto-generated
            customer.CreatedAt = DateTime.UtcNow;
            customer.OrganizationId = currentUser.OrganizationId;

            // Validate the model.
            ModelState.ClearValidationState(nameof(customer));
            if (!TryValidateModel(customer, nameof(customer)))
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

            _context.Customers.Add(customer);

            _context.SaveChanges();

            return Created("", customer);
        }

        // PATCH: odata/Customers(5)
        public IActionResult Patch([FromODataUri] int key, Delta<Customer> patch)
        {
            var currentUser = CurrentUser();

            var customer = _context.Customers
                .Where(c => c.OrganizationId == currentUser.OrganizationId)
                .Where(c => c.Id == key)
                .FirstOrDefault();

            // Ensure that object was found.
            if (customer == null) return NotFound();

            // Ensure that user is authorized.
            if (!currentUser.CanModifyCustomers ||
                currentUser.OrganizationId != customer.OrganizationId)
                return Forbid();

            // Do not allow modifying some properties.
            if (patch.GetChangedPropertyNames().Contains("OrganizationId") ||
                patch.GetChangedPropertyNames().Contains("Id") ||
                patch.GetChangedPropertyNames().Contains("CreatedAt"))
            {
                return BadRequest("Not authorized to modify the OrganizationId, CreatedAt, or Id.");
            }

            // Peform the update.
            patch.Patch(customer);

            // Validate the model.
            ModelState.ClearValidationState(nameof(customer));
            if (!TryValidateModel(customer, nameof(customer)))
                return BadRequest();

            _context.SaveChanges();

            return NoContent();
        }

        // DELETE: odata/Customers(5)
        public IActionResult Delete([FromODataUri] int key)
        {
            var currentUser = CurrentUser();

            var customer = _context.Customers
                .Where(c => c.OrganizationId == currentUser.OrganizationId)
                .Where(c => c.Id == key)
                .FirstOrDefault();

            // Ensure that object was found.
            if (customer == null) return NotFound();

            // Ensure that user is authorized.
            if (!currentUser.CanDeleteCustomers ||
                currentUser.OrganizationId != customer.OrganizationId)
                return Forbid();

            // Delete the object itself
            _context.Customers.Remove(customer);

            _context.SaveChanges();

            return NoContent();
        }

        // POST: odata/Customers/NextNumber
        [HttpPost]
        public IActionResult NextNumber()
        {
            var organizationId = CurrentUser().OrganizationId;
            var max = _context.Customers
                .Where(c => c.OrganizationId == organizationId)
                .Select(c => c.Number)
                .Max();
            if (max == null)
            {
                return Ok("1000");
            }
            else
            {
                var service = new SecurityService();
                var next = service.NxtKeyCode(max);
                return Ok(next);
            }
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