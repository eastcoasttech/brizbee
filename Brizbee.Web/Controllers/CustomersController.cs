using Brizbee.Common.Models;
using Brizbee.Repositories;
using Microsoft.OData;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Http;
using System.Web.OData;
using System.Web.OData.Extensions;

namespace Brizbee.Controllers
{
    public class CustomersController : BaseODataController
    {
        private BrizbeeWebContext db = new BrizbeeWebContext();
        private CustomerRepository repo = new CustomerRepository();

        // GET: odata/Customers
        [EnableQuery(PageSize = 20, MaxExpansionDepth = 1)]
        public IQueryable<Customer> GetPunches()
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
        public SingleResult<Customer> GetPunch([FromODataUri] int key)
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
                //Request.CreateErrorResponse(HttpStatusCode.NotFound, new ODataError() { Message = "" });
            }
        }

        // POST: odata/Customers
        public IHttpActionResult Post(Customer punch)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            punch = repo.Create(punch, CurrentUser());

            return Created(punch);
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