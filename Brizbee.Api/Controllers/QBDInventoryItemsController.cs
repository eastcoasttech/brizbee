//
//  QBDInventoryItemsController.cs
//  BRIZBEE API
//
//  Copyright (C) 2019-2021 East Coast Technology Services, LLC
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

using Brizbee.Core.Models;
using Brizbee.Core.Serialization;
using Dapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System.Globalization;
using System.Net;
using System.Text.Json;

namespace Brizbee.Api.Controllers
{
    public class QBDInventoryItemsController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        private readonly SqlContext _context;

        public QBDInventoryItemsController(IConfiguration configuration, SqlContext context)
        {
            _configuration = configuration;
            _context = context;
        }

        // GET: api/QBDInventoryItems
        [HttpGet("api/QBDInventoryItems")]
        public IActionResult GetQBDInventoryItems(
            [FromQuery] int skip = 0, [FromQuery] int pageSize = 1000,
            [FromQuery] string orderBy = "QBDINVENTORYITEMS/NAME", [FromQuery] string orderByDirection = "ASC")
        {
            if (pageSize > 1000) { return BadRequest(); }

            var currentUser = CurrentUser();

            // Ensure that user is authorized.
            if (!currentUser.CanViewInventoryItems)
                return Forbid();

            var total = 0;
            List<QBDInventoryItem> items = new List<QBDInventoryItem>(0);
            using (var connection = new SqlConnection(_configuration.GetConnectionString("SqlContext")))
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
                    case "QBDINVENTORYITEMS/CUSTOMBARCODEVALUE":
                        orderByFormatted = "I.[CustomBarCodeValue]";
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

            // Set headers for paging.
            HttpContext.Response.Headers.Add("X-Paging-PageSize", pageSize.ToString(CultureInfo.InvariantCulture));
            HttpContext.Response.Headers.Add("X-Paging-PageCount", pageCount.ToString(CultureInfo.InvariantCulture));
            HttpContext.Response.Headers.Add("X-Paging-TotalRecordCount", total.ToString(CultureInfo.InvariantCulture));

            return new JsonResult(items)
            {
                StatusCode = (int)HttpStatusCode.OK
            };
        }

        // GET: api/QBDInventoryItems/Search
        [HttpGet("api/QBDInventoryItems/Search")]
        public IActionResult GetQBDInventoryItemsSearch([FromQuery] string barCode)
        {
            var currentUser = CurrentUser();

            // Attempt to find an item based on the custom bar code
            var foundCustomItem = _context.QBDInventoryItems
                .FirstOrDefault(i => i.OrganizationId == currentUser.OrganizationId &&
                                     i.CustomBarCodeValue == barCode.Trim());

            // Attempt to find an item based on the QuickBooks Enterprise bar code
            var foundQuickBooksItem = _context.QBDInventoryItems
                .FirstOrDefault(i => i.OrganizationId == currentUser.OrganizationId &&
                                     i.BarCodeValue == barCode.Trim());

            if (foundCustomItem == null && foundQuickBooksItem == null)
                return NotFound();

            // Determine which item will be returned
            var chosenBarCodeValue = foundCustomItem != null ? foundCustomItem.CustomBarCodeValue : foundQuickBooksItem.BarCodeValue;
            var chosenItem = foundCustomItem ?? foundQuickBooksItem;

            if (chosenItem == null)
                return NotFound();

            // Return item with units of measure
            if (chosenItem.QBDUnitOfMeasureSetId.HasValue)
            {
                chosenItem.QBDUnitOfMeasureSet = _context.QBDUnitOfMeasureSets
                    .FirstOrDefault(s => s.Id == chosenItem.QBDUnitOfMeasureSetId);

                return Ok(new
                {
                    chosenItem.Id,
                    chosenItem.FullName,
                    BarCodeValue = chosenBarCodeValue,
                    chosenItem.ListId,
                    chosenItem.Name,
                    chosenItem.ManufacturerPartNumber,
                    chosenItem.PurchaseCost,
                    chosenItem.PurchaseDescription,
                    chosenItem.SalesPrice,
                    chosenItem.SalesDescription,
                    QBDUnitOfMeasureSet = new
                    {
                        chosenItem.QBDUnitOfMeasureSet.ListId,
                        chosenItem.QBDUnitOfMeasureSet.Name,
                        chosenItem.QBDUnitOfMeasureSet.UnitOfMeasureType,
                        UnitNamesAndAbbreviations = JsonSerializer.Deserialize<QuickBooksUnitOfMeasures>(chosenItem.QBDUnitOfMeasureSet.UnitNamesAndAbbreviations)
                    }
                });
            }
            else
            {
                return Ok(new
                {
                    chosenItem.Id,
                    chosenItem.FullName,
                    BarCodeValue = chosenBarCodeValue,
                    chosenItem.ListId,
                    chosenItem.Name,
                    chosenItem.ManufacturerPartNumber,
                    chosenItem.PurchaseCost,
                    chosenItem.PurchaseDescription,
                    chosenItem.SalesPrice,
                    chosenItem.SalesDescription
                });
            }
        }

        // POST: api/QBDInventoryItems/Sync
        [HttpPost("api/QBDInventoryItems/Sync")]
        public IActionResult PostSync(
            [FromBody] QBDInventoryItemSyncDetails details,
            [FromQuery] string productName,
            [FromQuery] string majorVersion,
            [FromQuery] string minorVersion,
            [FromQuery] string country,
            [FromQuery] string supportedQBXMLVersion,
            [FromQuery] string hostname,
            [FromQuery] string companyFilePath)
        {
            var currentUser = CurrentUser();

            // Ensure that user is authorized.
            if (!currentUser.CanSyncInventoryItems)
                return Forbid();

            // Ensure that the sync is for the same company file.
            var companyFileName = Path.GetFileName(companyFilePath);
            var previous = _context.QBDInventoryItemSyncs
                .Where(q => q.OrganizationId == currentUser.OrganizationId)
                .Where(q => q.HostCompanyFileName != companyFileName);

            if (previous.Any())
                return BadRequest("The company file appears to be different.");

            // Attempt to sync.
            using (var transaction = _context.Database.BeginTransaction())
            {
                try
                {
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
                        HostCompanyFileName = companyFileName,
                        HostCompanyFilePath = companyFilePath,
                        Hostname = hostname
                    };
                    _context.QBDInventoryItemSyncs.Add(sync);
                    _context.SaveChanges();


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
                            found.FullName = site.FullName;
                        }
                        else
                        {
                            // Create the inventory site
                            site.QBDInventoryItemSyncId = sync.Id;
                            _context.QBDInventorySites.Add(site);
                        }
                    }

                    _context.SaveChanges();


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

                    _context.SaveChanges();


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
                            found.OffsetItemFullName = string.IsNullOrEmpty(item.OffsetItemFullName) ? "" : item.OffsetItemFullName;

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
                            // Create the inventory item - massage properties if necessary.
                            item.QBDInventoryItemSyncId = sync.Id;
                            item.BarCodeValue = string.IsNullOrEmpty(item.BarCodeValue) ? "" : item.BarCodeValue;
                            item.CustomBarCodeValue = ""; // Default to blank
                            item.ManufacturerPartNumber = string.IsNullOrEmpty(item.ManufacturerPartNumber) ? "" : item.ManufacturerPartNumber;
                            item.PurchaseDescription = string.IsNullOrEmpty(item.PurchaseDescription) ? "" : item.PurchaseDescription;
                            item.SalesDescription = string.IsNullOrEmpty(item.SalesDescription) ? "" : item.SalesDescription;
                            item.OrganizationId = currentUser.OrganizationId;
                            item.OffsetItemFullName = string.IsNullOrEmpty(item.OffsetItemFullName) ? "" : item.OffsetItemFullName;

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

                    _context.SaveChanges();

                    transaction.Commit();

                    return Ok();
                }
                catch (DbUpdateException ex)
                {
                    transaction.Rollback();

                    return BadRequest(ex.Message);
                }
            }
        }

        // GET: api/QBDInventoryItems/5
        [HttpGet("api/QBDInventoryItems/{id}")]
        public IActionResult GetQBDInventoryItem(long id)
        {
            var currentUser = CurrentUser();

            // Ensure that user is authorized.
            if (!currentUser.CanViewInventoryItems)
                return Forbid();

            var inventoryItem = _context.QBDInventoryItems
                .Where(c => c.OrganizationId == currentUser.OrganizationId)
                .Where(c => c.Id == id)
                .Select(c => new
                {
                    c.Id,
                    c.Name,
                    c.FullName,
                    c.ManufacturerPartNumber,
                    c.BarCodeValue,
                    c.CustomBarCodeValue,
                    c.ListId,
                    c.PurchaseDescription,
                    c.PurchaseCost,
                    c.SalesDescription,
                    c.SalesPrice,
                    c.QBDUnitOfMeasureSetFullName,
                    c.QBDUnitOfMeasureSetListId,
                    c.QBDUnitOfMeasureSetId,
                    c.QBDInventoryItemSyncId,
                    c.QBDCOGSAccountFullName,
                    c.QBDCOGSAccountListId,
                    c.OffsetItemFullName,
                    c.OrganizationId
                })
                .FirstOrDefault();

            if (inventoryItem == null)
                return NotFound();

            return Ok(inventoryItem);
        }

        // PUT: api/QBDInventoryItems/5
        [HttpPut("api/QBDInventoryItems/{id}")]
        public IActionResult PutQBDInventoryItem(long id, [FromBody] QBDInventoryItem inventoryItem)
        {
            var currentUser = CurrentUser();

            // Ensure that user is authorized.
            if (!currentUser.CanModifyInventoryItems)
                return Forbid();

            var entity = _context.QBDInventoryItems
                .Where(c => c.OrganizationId == currentUser.OrganizationId)
                .Where(c => c.Id == id)
                .FirstOrDefault();

            if (entity == null)
                return NotFound();

            entity.CustomBarCodeValue = inventoryItem.CustomBarCodeValue;

            _context.SaveChanges();

            return Ok();
        }

        // DELETE: api/QBDInventoryItems/5
        [HttpDelete("api/QBDInventoryItems/{id:long}")]
        public async Task<IActionResult> Delete(long id)
        {
            var currentUser = CurrentUser();

            // Ensure that user is authorized.
            if (!currentUser.CanDeleteInventoryConsumptions)
                return Forbid();

            var inventoryItem = _context.QBDInventoryItems!
                .Where(c => c.OrganizationId == currentUser.OrganizationId)
                .FirstOrDefault(c => c.Id == id);

            if (inventoryItem == null)
            {
                return NotFound();
            }

            // Remove all the consumptions for this inventory item first.
            await _context.Database.GetDbConnection().ExecuteAsync(
                sql: "DELETE FROM [dbo].[QBDInventoryConsumptions] WHERE [QBDInventoryItemId] = @QBDInventoryItemId;",
                param: new
                {
                    QBDInventoryItemId = inventoryItem.Id
                });

            // Then remove the inventory item.
            _context.QBDInventoryItems!.Remove(inventoryItem);
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
