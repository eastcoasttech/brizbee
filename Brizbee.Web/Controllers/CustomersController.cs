﻿//
//  CustomersController.cs
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

using Brizbee.Common.Models;
using Microsoft.AspNet.OData;
using System;
using System.Linq;
using System.Net;
using System.Web.Http;

namespace Brizbee.Web.Controllers
{
    public class CustomersController : BaseODataController
    {
        private SqlContext db = new SqlContext();

        // GET: odata/Customers
        [EnableQuery(PageSize = 1000)]
        public IQueryable<Customer> GetCustomers()
        {
            var currentUser = CurrentUser();

            return db.Customers
                .Where(c => c.OrganizationId == currentUser.OrganizationId);
        }

        // GET: odata/Customers(5)
        [EnableQuery]
        public SingleResult<Customer> GetCustomer([FromODataUri] int key)
        {
            var currentUser = CurrentUser();

            return SingleResult.Create(db.Customers
                .Where(c => c.OrganizationId == currentUser.OrganizationId)
                .Where(c => c.Id == key));
        }

        // POST: odata/Customers
        public IHttpActionResult Post(Customer customer)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var currentUser = CurrentUser();

            // Ensure that user is authorized.
            if (!currentUser.CanCreateCustomers)
                return StatusCode(HttpStatusCode.Forbidden);

            // Auto-generated
            customer.CreatedAt = DateTime.UtcNow;
            customer.OrganizationId = currentUser.OrganizationId;

            db.Customers.Add(customer);

            db.SaveChanges();

            return Created(customer);
        }

        // PATCH: odata/Customers(5)
        [AcceptVerbs("PATCH", "MERGE")]
        public IHttpActionResult Patch([FromODataUri] int key, Delta<Customer> patch)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var currentUser = CurrentUser();

            var customer = db.Customers
                .Where(c => c.OrganizationId == currentUser.OrganizationId)
                .Where(c => c.Id == key)
                .FirstOrDefault();

            // Ensure that object was found.
            if (customer == null) return NotFound();

            // Ensure that user is authorized.
            if (!currentUser.CanModifyCustomers)
                return StatusCode(HttpStatusCode.Forbidden);

            // Do not allow modifying some properties.
            if (patch.GetChangedPropertyNames().Contains("OrganizationId"))
            {
                return BadRequest("Not authorized to modify the OrganizationId");
            }

            // Peform the update.
            patch.Patch(customer);

            db.SaveChanges();

            return Updated(customer);
        }

        // DELETE: odata/Customers(5)
        public IHttpActionResult Delete([FromODataUri] int key)
        {
            var currentUser = CurrentUser();

            var customer = db.Customers
                .Where(c => c.OrganizationId == currentUser.OrganizationId)
                .Where(c => c.Id == key)
                .FirstOrDefault();

            // Ensure that object was found.
            if (customer == null) return NotFound();

            // Ensure that user is authorized.
            if (!currentUser.CanDeleteCustomers)
                return StatusCode(HttpStatusCode.Forbidden);

            // Delete the object itself
            db.Customers.Remove(customer);

            db.SaveChanges();

            return StatusCode(HttpStatusCode.NoContent);
        }

        // POST: odata/Customers/Default.NextNumber
        public IHttpActionResult NextNumber()
        {
            var organizationId = CurrentUser().OrganizationId;
            var max = db.Customers
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