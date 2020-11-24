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
    [Route("api/[controller]")]
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
        [HttpGet]
        public ActionResult<IEnumerable<Customer>> GetCustomers(string order = "CreatedAt", string direction = "ASC", int pageNumber = 1, int pageSize = 1000)
        {
            // Determine the number of records to skip
            int skip = (pageNumber - 1) * pageSize;

            var currentUser = CurrentUser();

            // Validate order
            var allowed = new string[] { "CREATEDAT", "NAME", "NUMBER", "DESCRIPTION" };
            if (!allowed.Contains(order.ToUpperInvariant()))
            {
                return BadRequest();
            }

            // Validate direction
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

            // Get total number of records
            var total = _context.Customers
                .Where(c => c.OrganizationId == currentUser.OrganizationId)
                .Count();

            // Determine page count
            int pageCount = total > 0
                ? (int)Math.Ceiling(total / (double)pageSize)
                : 0;

            // Set headers for paging
            Response.Headers.Add("X-Paging-PageNumber", pageNumber.ToString(CultureInfo.InvariantCulture));
            Response.Headers.Add("X-Paging-PageSize", pageSize.ToString(CultureInfo.InvariantCulture));
            Response.Headers.Add("X-Paging-PageCount", pageCount.ToString(CultureInfo.InvariantCulture));
            Response.Headers.Add("X-Paging-TotalRecordCount", total.ToString(CultureInfo.InvariantCulture));

            return records;
        }

        // GET: api/Customers/5
        [HttpGet("{id}")]
        public async Task<ActionResult<Customer>> GetCustomer(int id)
        {
            var currentUser = CurrentUser();

            // Search within the organization
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
        [HttpPost]
        public async Task<ActionResult<Customer>> PostCustomer(Customer customer)
        {
            var currentUser = CurrentUser();

            // Ensure the same organization
            customer.OrganizationId = currentUser.OrganizationId;

            _context.Customers.Add(customer);
            await _context.SaveChangesAsync();

            return CreatedAtAction("GetCustomer", new { id = customer.Id }, customer);
        }

        // PUT: api/Users/5
        [HttpPut("{id}")]
        public IActionResult PutCustomer(int id, Customer patch)
        {
            var currentUser = CurrentUser();

            // Search within the organization
            var customer = _context.Customers
                .Where(c => c.OrganizationId == currentUser.OrganizationId)
                .FirstOrDefault();

            if (customer == null)
            {
                return NotFound();
            }

            // Apply the changes
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
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteCustomer(int id)
        {
            var currentUser = CurrentUser();

            // Only permit administrators to delete users
            if (currentUser.Role != "Administrator")
            {
                return BadRequest();
            }

            // Search within the organization
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
