using Brizbee.Common.Models;
using System;
using System.Collections.Generic;
using System.Data.Entity.Infrastructure;
using System.Data.Entity.Validation;
using System.Globalization;
using System.Linq;
using System.Web.Http;

namespace Brizbee.Web.Controllers
{
    public class InventoryAdjustmentsController : ApiController
    {
        private SqlContext db = new SqlContext();

        // GET: api/InventoryAdjustments/Unsynced
        [HttpGet]
        [Route("api/InventoryAdjustments/Unsynced")]
        public IHttpActionResult GetUnsynced()
        {
            IEnumerable<string> pageNumberHeaders;
            Request.Headers.TryGetValues("X-Paging-PageNumber", out pageNumberHeaders);

            IEnumerable<string> pageSizeHeaders;
            Request.Headers.TryGetValues("X-Paging-PageSize", out pageSizeHeaders);

            // Validate page number.
            if (pageNumberHeaders == null) { return BadRequest("Header X-Paging-PageNumber must be provided."); }
            var pageNumber = int.Parse(pageNumberHeaders.First(), CultureInfo.InvariantCulture);

            // Validate page size.
            if (pageSizeHeaders == null) { return BadRequest("Header X-Paging-PageSize must be provided."); }

            var pageSize = int.Parse(pageSizeHeaders.First(), CultureInfo.InvariantCulture);
            if (pageSize > 1000) { return BadRequest("Cannot exceed 1000 records per page in a single request"); }

            var currentUser = CurrentUser();

            var adjustments = db.InventoryAdjustments
                .Include("QBDInventoryItem")
                .Include("QBDInventorySite")
                .Where(a => a.OrganizationId == currentUser.OrganizationId)
                .Where(a => !a.QBDInventoryAdjustmentSyncId.HasValue);

            // Determine the number of records to skip.
            int skip = (pageNumber - 1) * pageSize;

            // Get total number of records.
            var total = adjustments.Count();

            // Determine page count.
            int pageCount = total > 0
                ? (int)Math.Ceiling(total / (double)pageSize)
                : 0;

            // Set headers for paging.
            Request.Headers.Add("X-Paging-PageNumber", pageNumber.ToString(CultureInfo.InvariantCulture));
            Request.Headers.Add("X-Paging-PageSize", pageSize.ToString(CultureInfo.InvariantCulture));
            Request.Headers.Add("X-Paging-PageCount", pageCount.ToString(CultureInfo.InvariantCulture));
            Request.Headers.Add("X-Paging-TotalRecordCount", total.ToString(CultureInfo.InvariantCulture));

            var records = adjustments
                .OrderBy(a => a.CreatedAt)
                .Skip(skip)
                .Take(pageSize)
                .ToList();

            return Ok(records);
        }

        // POST: api/InventoryAdjustments/Sync
        [HttpPost]
        [Route("api/InventoryAdjustments/Sync")]
        public IHttpActionResult PostSync([FromBody] int[] ids)
        {
            var currentUser = CurrentUser();

            var adjustments = db.InventoryAdjustments
                .Where(a => a.OrganizationId == currentUser.OrganizationId)
                .Where(a => !a.QBDInventoryAdjustmentSyncId.HasValue)
                .Where(a => ids.Contains(a.Id));

            // Record the sync.
            var sync = new QBDInventoryAdjustmentSync()
            {
                CreatedAt = DateTime.UtcNow,
                CreatedByUserId = currentUser.Id,
                OrganizationId = currentUser.OrganizationId
            };
            db.QBDInventoryAdjustmentSyncs.Add(sync);

            // Assign the adjustments to the sync.
            foreach (var adjustment in adjustments)
            {
                adjustment.QBDInventoryAdjustmentSyncId = adjustment.Id;
            }

            try
            {
                db.SaveChanges();

                return Ok();
            }
            catch (DbEntityValidationException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (DbUpdateConcurrencyException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (DbUpdateException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        private User CurrentUser()
        {
            if (ActionContext.RequestContext.Principal.Identity.Name.Length > 0)
            {
                var currentUserId = int.Parse(ActionContext.RequestContext.Principal.Identity.Name);
                return db.Users
                    .Where(u => u.Id == currentUserId)
                    .FirstOrDefault();
            }
            else
            {
                return null;
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
