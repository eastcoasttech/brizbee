using Brizbee.Common.Models;
using Brizbee.Common.Serialization;
using System;
using System.Data.Entity.Infrastructure;
using System.Data.Entity.Validation;
using System.Linq;
using System.Web.Http;

namespace Brizbee.Web.Controllers
{
    public class InventoryItemsController : ApiController
    {
        private SqlContext db = new SqlContext();

        // POST: api/InventoryItems/Sync
        [HttpPost]
        [Route("api/InventoryItems/Sync")]
        public IHttpActionResult PostSync([FromBody] QBDInventorySyncDetails details)
        {
            var currentUser = CurrentUser();

            var sync = new QBDInventoryItemSync()
            {
                CreatedAt = DateTime.UtcNow,
                CreatedByUserId = currentUser.Id,
                OrganizationId = currentUser.OrganizationId
            };


            // ----------------------------------------------------------------
            // Create or update the inventory sites
            // ----------------------------------------------------------------

            foreach (var site in details.InventorySites)
            {
                // Find by list id, which is unique across inventory sites
                var found = db.QBDInventorySites
                    .Where(s => s.ListId == site.ListId)
                    .FirstOrDefault();

                if (found != null)
                {
                    // Update any changes
                    found.Name = site.Name;
                }
                else
                {
                    // Create the inventory site
                    site.QBDInventoryItemSyncId = sync.Id;
                    db.QBDInventorySites.Add(site);
                }
            }


            // ----------------------------------------------------------------
            // Create or update the unit of measure sets
            // ----------------------------------------------------------------

            foreach (var unit in details.UnitOfMeasureSets)
            {
                // Find by list id, which is unique across unit of measure sets
                var found = db.QBDUnitOfMeasureSets
                    .Where(s => s.ListId == unit.ListId)
                    .FirstOrDefault();

                if (found != null)
                {
                    // Update any changes
                    found.Name = unit.Name;
                    found.BaseUnitName = unit.BaseUnitName;
                    found.BaseUnitAbbreviation = unit.BaseUnitAbbreviation;
                    found.IsActive = unit.IsActive;
                    found.UnitOfMeasureType = unit.UnitOfMeasureType;
                }
                else
                {
                    // Create the unit of measure set
                    unit.QBDInventoryItemSyncId = sync.Id;
                    db.QBDUnitOfMeasureSets.Add(unit);
                }
            }


            // ----------------------------------------------------------------
            // Create or update the inventory items
            // ----------------------------------------------------------------

            foreach (var item in details.InventoryItems)
            {
                // Find by list id, which is unique across inventory items
                var found = db.QBDInventoryItems
                    .Where(i => i.ListId == item.ListId)
                    .FirstOrDefault();

                if (found != null)
                {
                    // Update any changes
                    found.BarCodeValue = string.IsNullOrEmpty(item.BarCodeValue) ? "" : item.BarCodeValue;
                    found.ManufacturerPartNumber = string.IsNullOrEmpty(item.ManufacturerPartNumber) ? "" : item.ManufacturerPartNumber;
                    found.PurchaseDescription = string.IsNullOrEmpty(item.PurchaseDescription) ? "" : item.PurchaseDescription;
                    found.Name = item.Name;
                    found.FullName = item.FullName;
                    found.SalesDescription = string.IsNullOrEmpty(item.SalesDescription) ? "" : item.SalesDescription;
                }
                else
                {
                    // Create the inventory item
                    item.QBDInventoryItemSyncId = sync.Id;
                    item.BarCodeValue = string.IsNullOrEmpty(item.BarCodeValue) ? "" : item.BarCodeValue;
                    item.ManufacturerPartNumber = string.IsNullOrEmpty(item.ManufacturerPartNumber) ? "" : item.ManufacturerPartNumber;
                    item.PurchaseDescription = string.IsNullOrEmpty(item.PurchaseDescription) ? "" : item.PurchaseDescription;
                    item.SalesDescription = string.IsNullOrEmpty(item.SalesDescription) ? "" : item.SalesDescription;
                    db.QBDInventoryItems.Add(item);
                }
            }

            try
            {
                db.QBDInventoryItemSyncs.Add(sync);

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
                return BadRequest(ex.ToString());
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
