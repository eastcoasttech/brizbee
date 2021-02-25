//
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
using Dapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;

namespace Brizbee.Api.Controllers
{
    [ApiController]
    [Authorize]
    public class CustomersController : ControllerBase
    {
        private readonly IConfiguration Configuration;
        private readonly SqlContext _context;

        public CustomersController(IConfiguration configuration, SqlContext context)
        {
            Configuration = configuration;
            _context = context;
        }

        // GET: api/Customers
        [HttpGet("api/Customers")]
        public ActionResult<IEnumerable<Customer>> GetCustomers(string order = "CreatedAt", string direction = "ASC", int pageNumber = 1, int pageSize = 1000)
        {
            // Determine the number of records to skip.
            int skip = (pageNumber - 1) * pageSize;

            var currentUser = CurrentUser();

            // Validate order.
            var allowed = new string[] { "CREATEDAT", "NAME", "NUMBER", "DESCRIPTION" };
            if (!allowed.Contains(order.ToUpperInvariant()))
            {
                return BadRequest();
            }

            // Validate direction.
            direction = direction.ToUpperInvariant();
            if (direction != "ASC" || direction != "DESC")
            {
                return BadRequest();
            }

            var records = new List<Customer>();
            using (var connection = new SqlConnection(Configuration["ConnectionStrings:SqlContext"]))
            {
                connection.Open();

                var sql = $@"
                    SELECT
                        C.Id,
	                    C.CreatedAt,
	                    C.Name,
	                    C.Number,
                        C.Description,
	                    C.OrganizationId
                    FROM
	                    [Customers] AS C
                    WHERE
	                    C.[OrganizationId] = @OrganizationId
                    ORDER BY
	                    [{order}] {direction}
                    OFFSET @Skip ROWS
                    FETCH NEXT @PageSize ROWS ONLY;";

                records = connection.Query<Customer>(sql, new { OrganizationId = currentUser.OrganizationId, Skip = skip, PageSize = pageSize }).ToList();

                connection.Close();
            }

            // Get total number of records.
            var total = _context.Customers
                .Where(c => c.OrganizationId == currentUser.OrganizationId)
                .Count();

            // Determine page count.
            int pageCount = total > 0
                ? (int)Math.Ceiling(total / (double)pageSize)
                : 0;

            // Set headers for paging.
            Response.Headers.Add("X-Paging-PageNumber", pageNumber.ToString(CultureInfo.InvariantCulture));
            Response.Headers.Add("X-Paging-PageSize", pageSize.ToString(CultureInfo.InvariantCulture));
            Response.Headers.Add("X-Paging-PageCount", pageCount.ToString(CultureInfo.InvariantCulture));
            Response.Headers.Add("X-Paging-TotalRecordCount", total.ToString(CultureInfo.InvariantCulture));

            return records;
        }

        // GET: api/Customers/5
        [HttpGet("api/Customers/{id}")]
        public async Task<ActionResult<Customer>> GetCustomer(int id)
        {
            var currentUser = CurrentUser();

            // Find within the organization.
            var customer = await _context.Customers
                .Where(c => c.OrganizationId == currentUser.OrganizationId)
                .Where(c => c.Id == id)
                .FirstOrDefaultAsync();

            if (customer == null)
            {
                return NotFound();
            }

            return customer;
        }

        // POST: api/Customers
        [HttpPost("api/Customers")]
        public async Task<ActionResult<Customer>> PostCustomer(Customer customer)
        {
            var currentUser = CurrentUser();

            // Ensure the same organization.
            customer.OrganizationId = currentUser.OrganizationId;

            _context.Customers.Add(customer);
            await _context.SaveChangesAsync();

            return CreatedAtAction("GetCustomer", new { id = customer.Id }, customer);
        }

        // PUT: api/Customers/5
        [HttpPut("api/Customers/{id}")]
        public ActionResult PutCustomer(int id, Customer patch)
        {
            var currentUser = CurrentUser();

            // Ensure Administrator.
            if (currentUser.Role != "Administrator")
                return BadRequest();

            // Find within the organization.
            var customer = _context.Customers
                .Where(c => c.OrganizationId == currentUser.OrganizationId)
                .FirstOrDefault();

            if (customer == null)
            {
                return NotFound();
            }

            // Apply the changes.
            customer.Name = patch.Name;
            customer.Number = patch.Number;
            customer.Description = patch.Description;

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

        // DELETE: api/Customers/5
        [HttpDelete("api/Customers/{id}")]
        public async Task<ActionResult> DeleteCustomer(int id)
        {
            var currentUser = CurrentUser();

            // Ensure Administrator.
            if (currentUser.Role != "Administrator")
                return BadRequest();

            // Find within the organization.
            var customer = await _context.Customers
                .Where(c => c.OrganizationId == currentUser.OrganizationId)
                .Where(c => c.Id == id)
                .FirstOrDefaultAsync();

            if (customer == null)
            {
                return NotFound();
            }

            _context.Customers.Remove(customer);
            await _context.SaveChangesAsync();

            return NoContent();
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
