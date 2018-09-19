using Brizbee.Common.Models;
using Brizbee.Repositories;
using Microsoft.AspNet.OData;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Web.Http;

namespace Brizbee.Controllers
{
    public class CustomersController : BaseODataController
    {
        private BrizbeeWebContext db = new BrizbeeWebContext();
        private CustomerRepository repo = new CustomerRepository();

        // GET: odata/Customers
        [EnableQuery(PageSize = 20, MaxExpansionDepth = 1)]
        public IQueryable<Customer> GetCustomers()
        {
            try
            {
                return repo.GetAll(CurrentUser());
            }
            catch (Exception)
            {
                // Return an empty result if there are errors
                return Enumerable.Empty<Customer>().AsQueryable();
            }
        }

        // GET: odata/Customers(5)
        [EnableQuery]
        public SingleResult<Customer> GetCustomer([FromODataUri] int key)
        {
            try
            {
                var queryable = new List<Customer>() { repo.Get(key, CurrentUser()) }.AsQueryable();
                return SingleResult.Create(queryable);
            }
            catch (Exception)
            {
                // Return an empty result if there are errors
                return SingleResult.Create(Enumerable.Empty<Customer>().AsQueryable());
            }
        }

        // POST: odata/Customers
        public IHttpActionResult Post(Customer customer)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            customer = repo.Create(customer, CurrentUser());

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

            var customer = repo.Update(key, patch, CurrentUser());

            return Updated(customer);
        }

        // DELETE: odata/Customers(5)
        public IHttpActionResult Delete([FromODataUri] int key)
        {
            repo.Delete(key, CurrentUser());
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
            var service = new SecurityService();
            var next = service.NxtKeyCode(max);
            return Ok(next);
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