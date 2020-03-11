using Brizbee.Common.Database;
using Brizbee.Common.Exceptions;
using Brizbee.Common.Models;
using Brizbee.Web.Repositories;
using Microsoft.AspNet.OData;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Http;

namespace Brizbee.Web.Controllers
{
    public class RatesController : BaseODataController
    {
        private SqlContext db = new SqlContext();
        private RateRepository repo = new RateRepository();

        // GET: odata/Rates
        [EnableQuery(PageSize = 20, MaxExpansionDepth = 1)]
        public IQueryable<Rate> GetRates()
        {
            return repo.GetAll(CurrentUser());
        }

        // GET: odata/Rates(5)
        [EnableQuery]
        public SingleResult<Rate> GetRate([FromODataUri] int key)
        {
            return SingleResult.Create(repo.Get(key, CurrentUser()));
        }

        // POST: odata/Rates
        public IHttpActionResult Post(Rate rate)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            rate = repo.Create(rate, CurrentUser());

            return Created(rate);
        }

        // PATCH: odata/Rates(5)
        [AcceptVerbs("PATCH", "MERGE")]
        public IHttpActionResult Patch([FromODataUri] int key, Delta<Rate> patch)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var rate = repo.Update(key, patch, CurrentUser());

            return Updated(rate);
        }

        // DELETE: odata/Rates(5)
        public IHttpActionResult Delete([FromODataUri] int key)
        {
            repo.Delete(key, CurrentUser());
            return StatusCode(HttpStatusCode.NoContent);
        }

        // GET: odata/Rates/Default.BaseServiceRatesForPunches
        [HttpGet]
        [EnableQuery(PageSize = 30, MaxExpansionDepth = 1)]
        public IQueryable<Rate> BaseServiceRatesForPunches([FromODataUri] string InAt, [FromODataUri] string OutAt)
        {
            var inAt = DateTime.Parse(InAt);
            var outAt = DateTime.Parse(OutAt);
            var currentUser = CurrentUser();
            int[] userIds = db.Users
                .Where(u => u.OrganizationId == currentUser.OrganizationId)
                .Select(u => u.Id)
                .ToArray();
            var rateIds = db.Punches
                .Include("Task")
                .Where(p => userIds.Contains(p.UserId))
                .Where(p => p.InAt >= inAt && p.OutAt <= outAt)
                .Where(p => p.Task.BaseServiceRateId.HasValue)
                .Select(p => p.Task.BaseServiceRateId.Value);

            return db.Rates.Where(r => rateIds.Contains(r.Id));
        }

        // GET: odata/Rates/Default.BasePayrollRatesForPunches
        [HttpGet]
        [EnableQuery(PageSize = 30, MaxExpansionDepth = 1)]
        public IQueryable<Rate> BasePayrollRatesForPunches([FromODataUri] string InAt, [FromODataUri] string OutAt)
        {
            var inAt = DateTime.Parse(InAt);
            var outAt = DateTime.Parse(OutAt);
            var currentUser = CurrentUser();
            int[] userIds = db.Users
                .Where(u => u.OrganizationId == currentUser.OrganizationId)
                .Select(u => u.Id)
                .ToArray();
            var baseRateIds = db.Punches
                .Include("Task")
                .Where(p => userIds.Contains(p.UserId))
                .Where(p => p.InAt >= inAt && p.OutAt <= outAt)
                .Where(p => p.Task.BasePayrollRateId.HasValue)
                .Select(p => p.Task.BasePayrollRateId.Value);

            return db.Rates.Where(r => baseRateIds.Contains(r.Id));
        }

        // GET: odata/Rates/Default.AlternateServiceRatesForPunches
        [HttpGet]
        [EnableQuery(PageSize = 30, MaxExpansionDepth = 1)]
        public IQueryable<Rate> AlternateServiceRatesForPunches([FromODataUri] string InAt, [FromODataUri] string OutAt)
        {
            var inAt = DateTime.Parse(InAt);
            var outAt = DateTime.Parse(OutAt);
            var currentUser = CurrentUser();
            var userIds = db.Users
                .Where(u => u.OrganizationId == currentUser.OrganizationId)
                .Select(u => u.Id)
                .ToArray();
            var baseRateIds = db.Punches
                .Include("Task")
                .Where(p => userIds.Contains(p.UserId))
                .Where(p => p.InAt >= inAt && p.OutAt <= outAt)
                .Where(p => p.Task.BaseServiceRateId.HasValue)
                .GroupBy(p => p.Task.BaseServiceRateId)
                .Select(g => g.Key);

            return db.Rates.Where(r => baseRateIds.Contains(r.ParentRateId));
        }

        // GET: odata/Rates/Default.AlternatePayrollRatesForPunches
        [HttpGet]
        [EnableQuery(PageSize = 30, MaxExpansionDepth = 1)]
        public IQueryable<Rate> AlternatePayrollRatesForPunches([FromODataUri] string InAt, [FromODataUri] string OutAt)
        {
            var inAt = DateTime.Parse(InAt);
            var outAt = DateTime.Parse(OutAt);
            var currentUser = CurrentUser();
            var userIds = db.Users
                .Where(u => u.OrganizationId == currentUser.OrganizationId)
                .Select(u => u.Id)
                .ToArray();
            var baseRateIds = db.Punches
                .Include("Task")
                .Where(p => userIds.Contains(p.UserId))
                .Where(p => p.InAt >= inAt && p.OutAt <= outAt)
                .Where(p => p.Task.BasePayrollRateId.HasValue)
                .GroupBy(p => p.Task.BasePayrollRateId)
                .Select(g => g.Key);

            return db.Rates.Where(r => baseRateIds.Contains(r.ParentRateId));
        }

        /// <summary>
        /// Disposes of the resources used during each request (instance)
        /// of this controller.
        /// </summary>
        /// <param name="disposing">Whether or not the resources should be disposed</param>
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