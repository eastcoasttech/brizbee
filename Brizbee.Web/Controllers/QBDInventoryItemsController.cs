//
//  QBDInventoryItemsController.cs
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
using Brizbee.Common.Serialization;
using Dapper;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.Entity.Infrastructure;
using System.Data.Entity.Validation;
using System.Data.SqlClient;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;

namespace Brizbee.Web.Controllers
{
    public class QBDInventoryItemsController : ApiController
    {
        private SqlContext _context = new SqlContext();
        private JsonSerializerSettings settings = new JsonSerializerSettings()
        {
            NullValueHandling = NullValueHandling.Ignore,
            StringEscapeHandling = StringEscapeHandling.EscapeHtml,
            ReferenceLoopHandling = ReferenceLoopHandling.Ignore
        };

        // GET: api/QBDInventoryItems
        [HttpGet]
        [Route("api/QBDInventoryItems")]
        public HttpResponseMessage GetQBDInventoryItems(
            [FromUri] int skip = 0, [FromUri] int pageSize = 1000,
            [FromUri] string orderBy = "QBDINVENTORYITEMS/NAME", [FromUri] string orderByDirection = "ASC")
        {
            if (pageSize > 1000) { Request.CreateResponse(HttpStatusCode.BadRequest); }

            var currentUser = CurrentUser();

            // Ensure Administrator.
            if (currentUser.Role != "Administrator")
                Request.CreateResponse(HttpStatusCode.BadRequest);

            var total = 0;
            List<QBDInventoryItem> items = new List<QBDInventoryItem>();
            using (var connection = new SqlConnection(ConfigurationManager.ConnectionStrings["SqlContext"].ToString()))
            {
                connection.Open();

                // Determine the order by columns.
                var orderByFormatted = "";
                switch (orderBy.ToUpperInvariant())
                {
                    case "QBDINVENTORYITEMS/NAME":
                        orderByFormatted = "I.[Name]";
                        break;
                    case "QBDINVENTORYITEMS/MANUFACTURERPARTNUMBER":
                        orderByFormatted = "I.[ManufacturerPartNumber]";
                        break;
                    case "QBDINVENTORYITEMS/BARCODEVALUE":
                        orderByFormatted = "I.[BarCodeValue]";
                        break;
                    case "QBDINVENTORYITEMS/PURCHASEDESCRIPTION":
                        orderByFormatted = "I.[PurchaseDescription]";
                        break;
                    case "QBDINVENTORYITEMS/SALESDESCRIPTION":
                        orderByFormatted = "I.[SalesDescription]";
                        break;
                    default:
                        orderByFormatted = "I.[Name]";
                        break;
                }

                // Determine the order direction.
                var orderByDirectionFormatted = "";
                switch (orderByDirection.ToUpperInvariant())
                {
                    case "ASC":
                        orderByDirectionFormatted = "ASC";
                        break;
                    case "DESC":
                        orderByDirectionFormatted = "DESC";
                        break;
                    default:
                        orderByDirectionFormatted = "ASC";
                        break;
                }

                var parameters = new DynamicParameters();

                // Common clause.
                parameters.Add("@OrganizationId", currentUser.OrganizationId);

                // Get the count.
                var countSql = $@"
                    SELECT
                        COUNT(*)
                    FROM
                        [QBDInventoryItems] AS I
                    INNER JOIN
                        [QBDInventoryItemSyncs] AS S ON I.[QBDInventoryItemSyncId] = S.[Id]
                    WHERE
                        S.[OrganizationId] = @OrganizationId;";

                total = connection.QuerySingle<int>(countSql, parameters);

                // Paging parameters.
                parameters.Add("@Skip", skip);
                parameters.Add("@PageSize", pageSize);

                // Get the records.
                var recordsSql = $@"
                    SELECT
                        I.*
                    FROM
                        [QBDInventoryItems] AS I
                    INNER JOIN
                        [QBDInventoryItemSyncs] AS S ON I.[QBDInventoryItemSyncId] = S.[Id]
                    WHERE
                        S.[OrganizationId] = @OrganizationId
                    ORDER BY
                        {orderByFormatted} {orderByDirectionFormatted}
                    OFFSET @Skip ROWS
                    FETCH NEXT @PageSize ROWS ONLY;";

                var results = connection.Query<QBDInventoryItem>(recordsSql, parameters);

                items = results.ToList();

                connection.Close();
            }

            // Determine page count.
            int pageCount = total > 0
                ? (int)Math.Ceiling(total / (double)pageSize)
                : 0;

            // Create the response
            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(JsonConvert.SerializeObject(items, settings),
                    System.Text.Encoding.UTF8,
                    "application/json")
            };

            // Set headers for paging.
            response.Headers.Add("X-Paging-PageSize", pageSize.ToString(CultureInfo.InvariantCulture));
            response.Headers.Add("X-Paging-PageCount", pageCount.ToString(CultureInfo.InvariantCulture));
            response.Headers.Add("X-Paging-TotalRecordCount", total.ToString(CultureInfo.InvariantCulture));

            return response;
        }

        // GET: api/QBDInventoryItems/Search
        [HttpGet]
        [Route("api/QBDInventoryItems/Search")]
        public IHttpActionResult GetQBDInventoryItemsSearch([FromUri] string barCode)
        {
            var currentUser = CurrentUser();

            var item = _context.QBDInventoryItems
                .Where(i => i.OrganizationId == currentUser.OrganizationId)
                .Where(i => i.BarCodeValue == barCode)
                .FirstOrDefault();

            if (item == null)
                return NotFound();

            // Return item with units of measure
            if (item.QBDUnitOfMeasureSetId.HasValue)
            {
                item.QBDUnitOfMeasureSet = _context.QBDUnitOfMeasureSets
                    .Where(s => s.Id == item.QBDUnitOfMeasureSetId)
                    .FirstOrDefault();
            }

            return Ok(item);
        }

        // POST: api/QBDInventoryItems/Sync
        [HttpPost]
        [Route("api/QBDInventoryItems/Sync")]
        public IHttpActionResult PostSync(
            [FromBody] QBDInventorySyncDetails details,
            [FromUri] string productName,
            [FromUri] string majorVersion,
            [FromUri] string minorVersion,
            [FromUri] string country,
            [FromUri] string supportedQBXMLVersion,
            [FromUri] string hostname)
        {
            var currentUser = CurrentUser();

            var sync = new QBDInventoryItemSync()
            {
                CreatedAt = DateTime.UtcNow,
                CreatedByUserId = currentUser.Id,
                OrganizationId = currentUser.OrganizationId,
                HostProductName = productName,
                HostMajorVersion = majorVersion,
                HostMinorVersion = minorVersion,
                HostCountry = country,
                HostSupportedQBXMLVersion = supportedQBXMLVersion,
                Hostname = hostname
            };


            // ----------------------------------------------------------------
            // Create or update the inventory sites
            // ----------------------------------------------------------------

            foreach (var site in details.InventorySites)
            {
                // Find by list id, which is unique across inventory sites
                var found = _context.QBDInventorySites
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
                    _context.QBDInventorySites.Add(site);
                }
            }


            // ----------------------------------------------------------------
            // Create or update the unit of measure sets
            // ----------------------------------------------------------------

            foreach (var unit in details.UnitOfMeasureSets)
            {
                // Find by list id, which is unique across unit of measure sets
                var found = _context.QBDUnitOfMeasureSets
                    .Where(s => s.ListId == unit.ListId)
                    .FirstOrDefault();

                if (found != null)
                {
                    // Update any changes
                    found.Name = unit.Name;
                    found.UnitNamesAndAbbreviations = unit.UnitNamesAndAbbreviations;
                    found.IsActive = unit.IsActive;
                    found.UnitOfMeasureType = unit.UnitOfMeasureType;
                }
                else
                {
                    // Create the unit of measure set
                    unit.QBDInventoryItemSyncId = sync.Id;
                    _context.QBDUnitOfMeasureSets.Add(unit);
                }
            }


            // ----------------------------------------------------------------
            // Create or update the inventory items
            // ----------------------------------------------------------------

            foreach (var item in details.InventoryItems)
            {
                // Find by list id, which is unique across inventory items for the organization
                var found = _context.QBDInventoryItems
                    .Where(i => i.OrganizationId == currentUser.OrganizationId)
                    .Where(i => i.ListId == item.ListId)
                    .FirstOrDefault();

                if (found != null)
                {
                    // Update any changes.
                    found.BarCodeValue = string.IsNullOrEmpty(item.BarCodeValue) ? "" : item.BarCodeValue;
                    found.ManufacturerPartNumber = string.IsNullOrEmpty(item.ManufacturerPartNumber) ? "" : item.ManufacturerPartNumber;
                    found.PurchaseDescription = string.IsNullOrEmpty(item.PurchaseDescription) ? "" : item.PurchaseDescription;
                    found.PurchaseCost = item.PurchaseCost;
                    found.Name = item.Name;
                    found.FullName = item.FullName;
                    found.SalesDescription = string.IsNullOrEmpty(item.SalesDescription) ? "" : item.SalesDescription;
                    found.SalesPrice = item.SalesPrice;
                    found.QBDCOGSAccountFullName = item.QBDCOGSAccountFullName;
                    found.QBDCOGSAccountListId = item.QBDCOGSAccountListId;

                    // Associate the Unit of Measure Set.
                    found.QBDUnitOfMeasureSetFullName = string.IsNullOrEmpty(item.QBDUnitOfMeasureSetFullName) ? "" : item.QBDUnitOfMeasureSetFullName;
                    found.QBDUnitOfMeasureSetListId = string.IsNullOrEmpty(item.QBDUnitOfMeasureSetListId) ? "" : item.QBDUnitOfMeasureSetListId;

                    if (!string.IsNullOrEmpty(item.QBDUnitOfMeasureSetFullName))
                    {
                        var uomsId = _context.QBDUnitOfMeasureSets
                            .Where(u => u.ListId == found.QBDUnitOfMeasureSetListId)
                            .Select(u => u.Id)
                            .FirstOrDefault();
                        found.QBDUnitOfMeasureSetId = uomsId;
                    }
                }
                else
                {
                    // Create the inventory item.
                    item.QBDInventoryItemSyncId = sync.Id;
                    item.BarCodeValue = string.IsNullOrEmpty(item.BarCodeValue) ? "" : item.BarCodeValue;
                    item.ManufacturerPartNumber = string.IsNullOrEmpty(item.ManufacturerPartNumber) ? "" : item.ManufacturerPartNumber;
                    item.PurchaseDescription = string.IsNullOrEmpty(item.PurchaseDescription) ? "" : item.PurchaseDescription;
                    item.SalesDescription = string.IsNullOrEmpty(item.SalesDescription) ? "" : item.SalesDescription;
                    item.OrganizationId = currentUser.OrganizationId;

                    // Associate the Unit of Measure Set.
                    item.QBDUnitOfMeasureSetFullName = string.IsNullOrEmpty(item.QBDUnitOfMeasureSetFullName) ? "" : item.QBDUnitOfMeasureSetFullName;
                    item.QBDUnitOfMeasureSetListId = string.IsNullOrEmpty(item.QBDUnitOfMeasureSetListId) ? "" : item.QBDUnitOfMeasureSetListId;

                    if (!string.IsNullOrEmpty(item.QBDUnitOfMeasureSetFullName))
                    {
                        var uomsId = _context.QBDUnitOfMeasureSets
                            .Where(u => u.ListId == item.QBDUnitOfMeasureSetListId)
                            .Select(u => u.Id)
                            .FirstOrDefault();
                        item.QBDUnitOfMeasureSetId = uomsId;
                    }

                    _context.QBDInventoryItems.Add(item);
                }
            }

            try
            {
                _context.QBDInventoryItemSyncs.Add(sync);

                _context.SaveChanges();

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
                return _context.Users
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
                _context.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
