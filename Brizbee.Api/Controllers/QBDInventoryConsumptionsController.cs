//
//  QBDInventoryConsumptionsController.cs
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

using System.Collections;
using Brizbee.Api;
using Brizbee.Api.Serialization.Expanded;
using Brizbee.Core.Models;
using Brizbee.Core.Serialization;
using CsvHelper.Configuration;
using CsvHelper;
using Dapper;
using DocumentFormat.OpenXml.Math;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Primitives;
using System.Globalization;
using System.Net;
using System.Text;

namespace Brizbee.Api.Controllers
{
    public class QBDInventoryConsumptionsController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        private readonly SqlContext _context;

        public QBDInventoryConsumptionsController(IConfiguration configuration, SqlContext context)
        {
            _configuration = configuration;
            _context = context;
        }

        // GET: api/QBDInventoryConsumptions
        [HttpGet("api/QBDInventoryConsumptions")]
        public IActionResult GetQBDInventoryConsumptions(
            [FromQuery] int skip = 0, [FromQuery] int pageSize = 1000,
            [FromQuery] string orderBy = "QBDINVENTORYCONSUMPTIONS/CREATEDAT", [FromQuery] string orderByDirection = "ASC",
            [FromQuery] int[] jobIds = null, [FromQuery] string synced = "ALL")
        {
            if (pageSize > 1000) { return BadRequest(); }

            var currentUser = CurrentUser();

            // Ensure that user is authorized.
            if (!currentUser.CanViewInventoryConsumptions)
                return Forbid();

            var total = 0;
            List<QBDInventoryConsumption> consumptions = new List<QBDInventoryConsumption>(0);
            using (var connection = new SqlConnection(_configuration.GetConnectionString("SqlContext")))
            {
                connection.Open();

                // Determine the order by columns.
                var orderByFormatted = "";
                switch (orderBy.ToUpperInvariant())
                {
                    case "QBDINVENTORYCONSUMPTIONS/CREATEDAT":
                        orderByFormatted = "C.[CreatedAt]";
                        break;
                    case "QBDINVENTORYCONSUMPTIONS/QUANTITY":
                        orderByFormatted = "C.[Quantity]";
                        break;
                    case "QBDINVENTORYITEMS/NAME":
                        orderByFormatted = "I.[Name]";
                        break;
                    case "TASKS/NUMBER":
                        orderByFormatted = "T.[Number]";
                        break;
                    case "TASKS/NAME":
                        orderByFormatted = "T.[Name]";
                        break;
                    case "JOBS/NUMBER":
                        orderByFormatted = "J.[Number]";
                        break;
                    case "JOBS/NAME":
                        orderByFormatted = "J.[Name]";
                        break;
                    case "CUSTOMERS/NUMBER":
                        orderByFormatted = "CR.[Number]";
                        break;
                    case "CUSTOMERS/NAME":
                        orderByFormatted = "CR.[Name]";
                        break;
                    case "USERS/NAME":
                        orderByFormatted = "U.[Name]";
                        break;
                    default:
                        orderByFormatted = "C.[CreatedAt]";
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

                var whereClause = "";
                var parameters = new DynamicParameters();

                // Common clause.
                parameters.Add("@OrganizationId", currentUser.OrganizationId);

                // Clause for job ids.
                if (jobIds != null && jobIds.Any())
                {
                    whereClause += $" AND J.[Id] IN ({string.Join(",", jobIds)})";
                }

                // Clause for sync status.
                if (synced.ToUpperInvariant() == "SYNCED")
                {
                    whereClause += " AND C.QBDInventoryConsumptionSyncId IS NOT NULL";
                }
                else if (synced.ToUpperInvariant() == "UNSYNCED")
                {
                    whereClause += " AND C.QBDInventoryConsumptionSyncId IS NULL";
                }

                // Get the count.
                var countSql = $@"
                    SELECT
                        COUNT(*)
                    FROM
                        [QBDInventoryConsumptions] AS C
                    INNER JOIN
                        [Tasks] AS T ON C.[TaskId] = T.[Id]
                    INNER JOIN
                        [Jobs] AS J ON T.[JobId] = J.[Id]
                    WHERE
                        C.[OrganizationId] = @OrganizationId {whereClause};";

                total = connection.QuerySingle<int>(countSql, parameters);

                // Paging parameters.
                parameters.Add("@Skip", skip);
                parameters.Add("@PageSize", pageSize);

                // Get the records.
                var recordsSql = $@"
                    SELECT
                        C.Id AS Consumption_Id,
                        C.CreatedAt AS Consumption_CreatedAt,
                        C.Hostname AS Consumption_Hostname,
                        C.OrganizationId AS Consumption_OrganizationId,
                        C.UnitOfMeasure AS Consumption_UnitOfMeasure,
                        C.Quantity AS Consumption_Quantity,
                        C.QBDInventoryItemId AS Consumption_QBDInventoryItemId,
                        C.QBDInventoryConsumptionSyncId AS Consumption_QBDInventoryConsumptionSyncId,
                        C.CreatedByUserId AS Consumption_CreatedByUserId,
                        C.QBDInventorySiteId AS Consumption_QBDInventorySiteId,

                        I.Id AS Item_Id,
                        I.Name AS Item_Name,
                        I.ManufacturerPartNumber AS Item_ManufacturerPartNumber,
                        I.BarCodeValue AS Item_BarCodeValue,
                        I.PurchaseDescription AS Item_PurchaseDescription,
                        I.SalesDescription AS Item_SalesDescription,
                        I.QBDInventoryItemSyncId AS Item_QBDInventoryItemSyncId,
                        I.FullName AS Item_FullName,
                        I.ListId AS Item_ListId,
                        I.PurchaseCost AS Item_PurchaseCost,
                        I.SalesPrice AS Item_SalesPrice,

                        T.Name as Task_Name,
                        T.Number as Task_Number,

                        J.Name as Job_Name,
                        J.Number as Job_Number,

                        CR.Name as Customer_Name,
                        CR.Number as Customer_Number,

                        U.Id AS User_Id,
                        U.Name AS User_Name
                    FROM
                        [QBDInventoryConsumptions] AS C
                    INNER JOIN
                        [QBDInventoryItems] AS I ON C.[QBDInventoryItemId] = I.[Id]
                    INNER JOIN
                        [Tasks] AS T ON C.[TaskId] = T.[Id]
                    INNER JOIN
                        [Jobs] AS J ON T.[JobId] = J.[Id]
                    INNER JOIN
                        [Customers] AS CR ON J.[CustomerId] = CR.[Id]
                    INNER JOIN
                        [Users] AS U ON C.[CreatedByUserId] = U.[Id]
                    WHERE
                        C.[OrganizationId] = @OrganizationId {whereClause}
                    ORDER BY
                        {orderByFormatted} {orderByDirectionFormatted}
                    OFFSET @Skip ROWS
                    FETCH NEXT @PageSize ROWS ONLY;";

                var results = connection.Query<QBDInventoryConsumptionExpanded>(recordsSql, parameters);

                foreach (var result in results)
                {
                    consumptions.Add(new QBDInventoryConsumption()
                    {
                        Id = result.Consumption_Id,
                        CreatedAt = result.Consumption_CreatedAt,
                        Hostname = result.Consumption_Hostname,
                        OrganizationId = result.Consumption_OrganizationId,
                        UnitOfMeasure = result.Consumption_UnitOfMeasure,
                        Quantity = result.Consumption_Quantity,
                        QBDInventoryItemId = result.Consumption_QBDInventoryItemId,
                        QBDInventoryConsumptionSyncId = result.Consumption_QBDInventoryConsumptionSyncId,
                        CreatedByUserId = result.Consumption_CreatedByUserId,
                        QBDInventorySiteId = result.Consumption_QBDInventorySiteId,
                        QBDInventoryItem = new QBDInventoryItem()
                        {
                            Id = result.Item_Id,
                            Name = result.Item_Name,
                            ManufacturerPartNumber = result.Item_ManufacturerPartNumber,
                            BarCodeValue = result.Item_BarCodeValue,
                            PurchaseDescription = result.Item_PurchaseDescription,
                            SalesDescription = result.Item_SalesDescription,
                            QBDInventoryItemSyncId = result.Item_QBDInventoryItemSyncId,
                            FullName = result.Item_FullName,
                            ListId = result.Item_ListId,
                            PurchaseCost = result.Item_PurchaseCost,
                            SalesPrice = result.Item_SalesPrice
                        },
                        Task = new Brizbee.Core.Models.Task()
                        {
                            Number = result.Task_Number,
                            Name = result.Task_Name,
                            Job = new Job()
                            {
                                Number = result.Job_Number,
                                Name = result.Job_Name,
                                Customer = new Customer()
                                {
                                    Number = result.Customer_Number,
                                    Name = result.Customer_Name
                                }
                            }
                        },
                        CreatedByUser = new User()
                        {
                            Id = result.User_Id,
                            Name = result.User_Name
                        }
                    });
                }

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

            return new JsonResult(consumptions)
            {
                StatusCode = (int)HttpStatusCode.OK
            };
        }

        // GET: api/QBDInventoryConsumptions/Unsynced
        [HttpGet("api/QBDInventoryConsumptions/Unsynced")]
        public IActionResult GetUnsynced()
        {
            StringValues pageNumberHeaders;
            HttpContext.Request.Headers.TryGetValue("X-Paging-PageNumber", out pageNumberHeaders);

            StringValues pageSizeHeaders;
            HttpContext.Request.Headers.TryGetValue("X-Paging-PageSize", out pageSizeHeaders);

            // Validate page number.
            if (string.IsNullOrEmpty(pageNumberHeaders))
                return BadRequest("Header X-Paging-PageNumber must be provided.");

            var pageNumber = int.Parse(pageNumberHeaders.First(), CultureInfo.InvariantCulture);

            // Validate page size.
            if (string.IsNullOrEmpty(pageSizeHeaders))
                return BadRequest("Header X-Paging-PageSize must be provided.");

            var pageSize = int.Parse(pageSizeHeaders.First(), CultureInfo.InvariantCulture);
            if (pageSize > 1000)
                return BadRequest("Cannot exceed 1000 records per page in a single request.");

            var currentUser = CurrentUser();

            // Ensure that user is authorized.
            if (!currentUser.CanViewInventoryConsumptions)
                return BadRequest();

            var consumptions = _context.QBDInventoryConsumptions
                .Include("QBDInventoryItem")
                .Include("Task")
                .Include("Task.Job")
                //.Include("QBDInventorySite")
                //.DefaultIfEmpty()
                .Where(c => c.OrganizationId == currentUser.OrganizationId)
                .Where(c => !c.QBDInventoryConsumptionSyncId.HasValue);

            // Determine the number of records to skip.
            int skip = (pageNumber - 1) * pageSize;

            // Get total number of records.
            var total = consumptions.Count();

            // Determine page count.
            int pageCount = total > 0
                ? (int)Math.Ceiling(total / (double)pageSize)
                : 0;

            var records = consumptions
                .OrderBy(a => a.CreatedAt)
                .Skip(skip)
                .Take(pageSize)
                .ToList();

            // Set headers for paging.
            HttpContext.Response.Headers.Add("X-Paging-PageNumber", pageNumber.ToString(CultureInfo.InvariantCulture));
            HttpContext.Response.Headers.Add("X-Paging-PageSize", pageSize.ToString(CultureInfo.InvariantCulture));
            HttpContext.Response.Headers.Add("X-Paging-PageCount", pageCount.ToString(CultureInfo.InvariantCulture));
            HttpContext.Response.Headers.Add("X-Paging-TotalRecordCount", total.ToString(CultureInfo.InvariantCulture));

            return new JsonResult(records)
            {
                StatusCode = (int)HttpStatusCode.OK
            };
        }
        
        // GET: api/QBDInventoryConsumptions/Export
        [HttpGet("api/QBDInventoryConsumptions/Export")]
        public IActionResult GetExport([FromQuery] DateTime minCreatedAt, [FromQuery] DateTime maxCreatedAt)
        {
            var currentUser = CurrentUser();

            // Ensure that user is authorized.
            if (!currentUser.CanViewInventoryConsumptions)
                return BadRequest();

            var consumptions = _context.QBDInventoryConsumptions!
                .Include(c => c.QBDInventoryItem)
                .Include(c => c.Task)
                .Include(c => c.Task!.Job)
                .Where(c => c.OrganizationId == currentUser.OrganizationId)
                .Where(c => c.CreatedAt >= minCreatedAt && c.CreatedAt <= maxCreatedAt)
                .OrderBy(c => c.CreatedAt)
                .ToList();
            
            var configuration = new CsvConfiguration(CultureInfo.CurrentCulture)
            {
                Delimiter = ","
            };

            using var writer = new StringWriter();
            using var csv = new CsvWriter(writer, configuration);

            csv.WriteRecords((IEnumerable)consumptions);

            var bytes = Encoding.UTF8.GetBytes(writer.ToString());
            return File(bytes, "text/csv", fileDownloadName:
                $"Consumption {minCreatedAt.ToShortDateString()} thru {maxCreatedAt.ToShortDateString()}.csv");
        }

        // POST: api/QBDInventoryConsumptions/Sync
        [HttpPost("api/QBDInventoryConsumptions/Sync")]
        public IActionResult PostSync(
            [FromBody] QBDConsumptionSyncDetails details,
            [FromQuery] string productName,
            [FromQuery] string majorVersion,
            [FromQuery] string minorVersion,
            [FromQuery] string country,
            [FromQuery] string supportedQBXMLVersion,
            [FromQuery] string recordingMethod,
            [FromQuery] string valueMethod,
            [FromQuery] string hostname,
            [FromQuery] string companyFilePath)
        {
            var currentUser = CurrentUser();

            // Ensure that user is authorized.
            if (!currentUser.CanSyncInventoryConsumptions)
                return Forbid();

            // Ensure that the sync is for the same company file.
            var companyFileName = Path.GetFileName(companyFilePath);
            var previous = _context.QBDInventoryConsumptionSyncs
                .Where(q => q.OrganizationId == currentUser.OrganizationId)
                .Where(q => q.HostCompanyFileName != companyFileName);

            if (previous.Any())
                return BadRequest("The company file appears to be different.");

            // Attempt to sync.
            var consumptions = _context.QBDInventoryConsumptions
                .Where(a => a.OrganizationId == currentUser.OrganizationId)
                .Where(a => !a.QBDInventoryConsumptionSyncId.HasValue)
                .Where(a => details.Ids.Contains(a.Id))
                .ToList();

            // Record the sync.
            var sync = new QBDInventoryConsumptionSync()
            {
                CreatedAt = DateTime.UtcNow,
                CreatedByUserId = currentUser.Id,
                OrganizationId = currentUser.OrganizationId,
                RecordingMethod = recordingMethod,
                ValueMethod = valueMethod,
                HostProductName = productName,
                HostMajorVersion = majorVersion,
                HostMinorVersion = minorVersion,
                HostCountry = country,
                HostSupportedQBXMLVersion = supportedQBXMLVersion,
                HostCompanyFileName = companyFileName,
                HostCompanyFilePath = companyFilePath,
                ConsumptionsCount = consumptions.Count,
                Hostname = hostname,
                TxnIDs = string.Join(",", details.TxnIDs)
            };
            _context.QBDInventoryConsumptionSyncs.Add(sync);

            // Assign the consumptions to the sync.
            foreach (var consumption in consumptions)
            {
                consumption.QBDInventoryConsumptionSyncId = sync.Id;
            }

            try
            {
                _context.SaveChanges();

                return Ok();
            }
            catch (DbUpdateException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        // POST: api/QBDInventoryConsumptions/Consume
        [HttpPost("api/QBDInventoryConsumptions/Consume")]
        public IActionResult PostConsume([FromQuery] long qbdInventoryItemId, [FromQuery] int quantity, [FromQuery] string hostname, [FromQuery] string unitOfMeasure = "")
        {
            var currentUser = CurrentUser();
            const bool inventorySiteEnabled = false;

            // Find the current punch.
            var currentPunch = _context.Punches
                .Where(p => p.UserId == currentUser.Id)
                .Where(p => p.OutAt == null)
                .OrderByDescending(p => p.InAt)
                .FirstOrDefault();

            if (currentPunch == null)
                return BadRequest("Cannot consume inventory without being punched in.");

            if (quantity < 1)
                return BadRequest("Cannot consume inventory with negative or zero quantity.");

            long? siteId = null;
            if (inventorySiteEnabled)
            {
                // Inventory site is determined by the hostname.
                var sites = new Dictionary<string, string>()
                {
                    { "MARTIN-01", "" }
                };
                var siteForHostname = sites[hostname];

                // Ensure there is an inventory site for the given hostname, if necessary.
                if (string.IsNullOrEmpty(siteForHostname))
                    return BadRequest($"No inventory site for hostname {hostname} specified");

                siteId = _context.QBDInventorySites
                    .Where(s => s.FullName == siteForHostname)
                    .Select(s => s.Id)
                    .FirstOrDefault();
            }

            var consumption = new QBDInventoryConsumption()
            {
                CreatedAt = DateTime.UtcNow,
                CreatedByUserId = currentUser.Id,
                OrganizationId = currentUser.OrganizationId,
                Quantity = quantity,
                TaskId = currentPunch.TaskId,
                QBDInventoryItemId = qbdInventoryItemId,
                Hostname = hostname,
                QBDInventorySiteId = siteId,
                UnitOfMeasure = unitOfMeasure
            };

            _context.QBDInventoryConsumptions!.Add(consumption);
            _context.SaveChanges();

            return Ok(consumption);
        }

        // GET: api/QBDInventoryConsumptions/5
        [HttpGet("api/QBDInventoryConsumptions/{id}")]
        public IActionResult Get(long id)
        {
            var currentUser = CurrentUser();

            // Ensure that user is authorized.
            if (!currentUser.CanViewInventoryConsumptions)
                return Forbid();

            var consumption = _context.QBDInventoryConsumptions
                .Include("CreatedByUser")
                .Include("QBDInventoryItem")
                .Where(c => c.OrganizationId == currentUser.OrganizationId)
                .Where(c => c.Id == id)
                .Select(c => new
                {
                    c.Id,
                    c.CreatedByUserId,
                    c.CreatedAt,
                    c.Hostname,
                    c.OrganizationId,
                    c.QBDInventoryConsumptionSyncId,
                    c.QBDInventoryItemId,
                    c.QBDInventorySiteId,
                    c.QBDUnitOfMeasureSetId,
                    c.Quantity,
                    c.TaskId,
                    c.UnitOfMeasure,
                    CreatedByUser = new
                    {
                        c.CreatedByUser.Id,
                        c.CreatedByUser.Name
                    },
                    QBDInventoryItem = new
                    {
                        c.QBDInventoryItem.FullName,
                        c.QBDInventoryItem.Name,
                        c.QBDInventoryItem.Id,
                        c.QBDInventoryItem.ListId,
                        c.QBDInventoryItem.ManufacturerPartNumber,
                        c.QBDInventoryItem.OffsetItemFullName,
                        c.QBDInventoryItem.BarCodeValue,
                        c.QBDInventoryItem.PurchaseCost,
                        c.QBDInventoryItem.PurchaseDescription,
                        c.QBDInventoryItem.SalesPrice,
                        c.QBDInventoryItem.SalesDescription,
                        c.QBDInventoryItem.QBDCOGSAccountFullName
                    }
                })
                .FirstOrDefault();

            if (consumption == null)
                return NotFound();

            return Ok(consumption);
        }
        
        // PUT: api/QBDInventoryConsumptions/5
        [HttpPut("api/QBDInventoryConsumptions/{id}")]
        public IActionResult PutQbdInventoryConsumption(long id, [FromBody] QBDInventoryConsumption inventoryConsumption)
        {
            var currentUser = CurrentUser();

            // Ensure that user is authorized.
            if (!currentUser.CanDeleteInventoryConsumptions)
                return Forbid();

            var entity = _context.QBDInventoryConsumptions!
                .Where(c => c.OrganizationId == currentUser.OrganizationId)
                .FirstOrDefault(c => c.Id == id);

            if (entity == null)
            {
                return NotFound();
            }

            if (entity.Quantity < 1)
            {
                return BadRequest("Cannot consume inventory with negative or zero quantity.");
            }

            entity.Quantity = inventoryConsumption.Quantity;

            _context.SaveChanges();

            return Ok();
        }

        // DELETE: api/QBDInventoryConsumptions/5
        [HttpDelete("api/QBDInventoryConsumptions/{id}")]
        public IActionResult Delete(long id)
        {
            var currentUser = CurrentUser();

            // Ensure that user is authorized.
            if (!currentUser.CanDeleteInventoryConsumptions)
                return Forbid();

            var consumption = _context.QBDInventoryConsumptions
                .Where(c => c.OrganizationId == currentUser.OrganizationId)
                .Where(c => c.Id == id)
                .FirstOrDefault();

            if (consumption == null) return NotFound();

            _context.QBDInventoryConsumptions.Remove(consumption);
            _context.SaveChanges();

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
