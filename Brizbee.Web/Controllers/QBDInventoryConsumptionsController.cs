//
//  QBDInventoryConsumptionsController.cs
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
using Brizbee.Web.Serialization.Expanded;
using Dapper;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.Entity.Infrastructure;
using System.Data.Entity.Validation;
using System.Data.SqlClient;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;

namespace Brizbee.Web.Controllers
{
    public class QBDInventoryConsumptionsController : ApiController
    {
        private SqlContext _context = new SqlContext();
        private JsonSerializerSettings settings = new JsonSerializerSettings()
        {
            NullValueHandling = NullValueHandling.Ignore,
            StringEscapeHandling = StringEscapeHandling.EscapeHtml,
            ReferenceLoopHandling = ReferenceLoopHandling.Ignore
        };

        // GET: api/QBDInventoryConsumptions
        [HttpGet]
        [Route("api/QBDInventoryConsumptions")]
        public HttpResponseMessage GetQBDInventoryConsumptions(
            [FromUri] int skip = 0, [FromUri] int pageSize = 1000,
            [FromUri] string orderBy = "QBDINVENTORYCONSUMPTIONS/CREATEDAT", [FromUri] string orderByDirection = "ASC",
            [FromUri] int[] jobIds = null, [FromUri] string synced = "ALL")
        {
            if (pageSize > 1000) { Request.CreateResponse(HttpStatusCode.BadRequest); }

            var currentUser = CurrentUser();

            // Ensure Administrator.
            if (currentUser.Role != "Administrator")
                Request.CreateResponse(HttpStatusCode.BadRequest);

            var total = 0;
            List<QBDInventoryConsumption> consumptions = new List<QBDInventoryConsumption>();
            using (var connection = new SqlConnection(ConfigurationManager.ConnectionStrings["SqlContext"].ToString()))
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
                        Task = new Task()
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

            // Create the response
            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(JsonConvert.SerializeObject(consumptions, settings),
                    System.Text.Encoding.UTF8,
                    "application/json")
            };

            // Set headers for paging.
            response.Headers.Add("X-Paging-PageSize", pageSize.ToString(CultureInfo.InvariantCulture));
            response.Headers.Add("X-Paging-PageCount", pageCount.ToString(CultureInfo.InvariantCulture));
            response.Headers.Add("X-Paging-TotalRecordCount", total.ToString(CultureInfo.InvariantCulture));

            return response;
        }

        // GET: api/QBDInventoryConsumptions/Unsynced
        [HttpGet]
        [Route("api/QBDInventoryConsumptions/Unsynced")]
        public HttpResponseMessage GetUnsynced()
        {
            IEnumerable<string> pageNumberHeaders;
            Request.Headers.TryGetValues("X-Paging-PageNumber", out pageNumberHeaders);

            IEnumerable<string> pageSizeHeaders;
            Request.Headers.TryGetValues("X-Paging-PageSize", out pageSizeHeaders);

            // Validate page number.
            if (pageNumberHeaders == null)
                Request.CreateResponse(HttpStatusCode.BadRequest, "Header X-Paging-PageNumber must be provided.");

            var pageNumber = int.Parse(pageNumberHeaders.First(), CultureInfo.InvariantCulture);

            // Validate page size.
            if (pageSizeHeaders == null)
                Request.CreateResponse(HttpStatusCode.BadRequest, "Header X-Paging-PageSize must be provided.");

            var pageSize = int.Parse(pageSizeHeaders.First(), CultureInfo.InvariantCulture);
            if (pageSize > 1000)
                Request.CreateResponse(HttpStatusCode.BadRequest, "Cannot exceed 1000 records per page in a single request.");

            var currentUser = CurrentUser();

            // Ensure Administrator.
            if (currentUser.Role != "Administrator")
                Request.CreateResponse(HttpStatusCode.BadRequest);

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

            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(JsonConvert.SerializeObject(records, settings),
                System.Text.Encoding.UTF8,
                "application/json")
            };

            // Set headers for paging.
            response.Headers.Add("X-Paging-PageNumber", pageNumber.ToString(CultureInfo.InvariantCulture));
            response.Headers.Add("X-Paging-PageSize", pageSize.ToString(CultureInfo.InvariantCulture));
            response.Headers.Add("X-Paging-PageCount", pageCount.ToString(CultureInfo.InvariantCulture));
            response.Headers.Add("X-Paging-TotalRecordCount", total.ToString(CultureInfo.InvariantCulture));

            return response;
        }

        // POST: api/QBDInventoryConsumptions/Sync
        [HttpPost]
        [Route("api/QBDInventoryConsumptions/Sync")]
        public IHttpActionResult PostSync(
            [FromBody] QBDConsumptionSyncDetails details,
            [FromUri] string productName,
            [FromUri] string majorVersion,
            [FromUri] string minorVersion,
            [FromUri] string country,
            [FromUri] string supportedQBXMLVersion,
            [FromUri] string recordingMethod,
            [FromUri] string valueMethod,
            [FromUri] string hostname,
            [FromUri] string companyFilePath)
        {
            var currentUser = CurrentUser();

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
            catch (DbEntityValidationException ex)
            {
                string message = "";

                foreach (var eve in ex.EntityValidationErrors)
                {
                    foreach (var ve in eve.ValidationErrors)
                    {
                        message += string.Format("{0} has error '{1}'; ", ve.PropertyName, ve.ErrorMessage);
                    }
                }

                return BadRequest(message);
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

        // POST: api/QBDInventoryConsumptions/Consume
        [HttpPost]
        [Route("api/QBDInventoryConsumptions/Consume")]
        public IHttpActionResult PostConsume([FromUri] long qbdInventoryItemId, [FromUri] int quantity, [FromUri] string hostname, [FromUri] string unitOfMeasure = "")
        {
            var currentUser = CurrentUser();
            var inventorySiteEnabled = false;

            // Find the current punch.
            var currentPunch = _context.Punches
                .Where(p => p.UserId == currentUser.Id)
                .Where(p => p.OutAt == null)
                .OrderByDescending(p => p.InAt)
                .FirstOrDefault();

            if (currentPunch == null)
                return BadRequest("Cannot consume inventory without being punched in.");

            long? siteId = null;
            if (inventorySiteEnabled)
            {
                // Inventory site is determined by the hostname.
                var sites = new Dictionary<string, string>()
                {
                    { "MARTIN-01", "" },
                    { "RR01", "" },
                    { "RR08", "" },
                    { "RR09", "" },
                    { "RR11", "" },
                    { "RR12", "" },
                    { "RR23", "" },
                    { "RR24", "" },
                    { "RR25", "" },
                    { "RR26", "" },
                    { "RR27", "" },
                    { "RR28", "" },
                    { "RR17", "" },
                    { "RR16", "" },
                    { "RR18", "" },
                    { "RR19", "" },
                    { "RR02", "" },
                    { "RR03", "" },
                    { "RR05", "" },
                    { "RR06", "" },
                    { "RR04", "" },
                    { "RR15", "" },
                    { "Pocket_PC", "" }
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

            _context.QBDInventoryConsumptions.Add(consumption);
            _context.SaveChanges();

            return Ok();
        }

        // GET: api/QBDInventoryConsumptions/5
        public IHttpActionResult GetQBDInventoryConsumption(long id)
        {
            var currentUser = CurrentUser();

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

        // DELETE: api/QBDInventoryConsumptions/5
        public IHttpActionResult DeleteQBDInventoryConsumption(long id)
        {
            var currentUser = CurrentUser();

            var consumption = _context.QBDInventoryConsumptions
                .Where(c => c.OrganizationId == currentUser.OrganizationId)
                .Where(c => c.Id == id)
                .FirstOrDefault();

            if (consumption == null)
                return NotFound();

            _context.QBDInventoryConsumptions.Remove(consumption);
            _context.SaveChanges();

            return StatusCode(HttpStatusCode.NoContent);
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
