using Brizbee.Common.Models;
using Brizbee.Repositories;
using Microsoft.AspNet.OData;
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
        [EnableQuery(PageSize = 20)]
        public IQueryable<Customer> GetCustomers()
        {
            return repo.GetAll(CurrentUser());
        }

        // GET: odata/Customers(5)
        [EnableQuery]
        public SingleResult<Customer> GetCustomer([FromODataUri] int key)
        {
            return SingleResult.Create(repo.Get(key, CurrentUser()));
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
                repo.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}