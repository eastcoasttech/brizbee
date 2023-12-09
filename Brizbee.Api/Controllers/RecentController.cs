//
//  RecentController.cs
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

using Brizbee.Api.Serialization.Expanded;
using Brizbee.Core.Models;
using Dapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;

namespace Brizbee.Api.Controllers
{
    public class RecentController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        private readonly SqlContext _context;

        public RecentController(IConfiguration configuration, SqlContext context)
        {
            _configuration = configuration;
            _context = context;
        }

        // GET: api/Recent/Punches
        [HttpGet("api/Recent/Punches")]
        public IActionResult GetRecentPunches()
        {
            var currentUser = CurrentUser();

            List<Punch> punches = new List<Punch>(0);
            using (var connection = new SqlConnection(_configuration.GetConnectionString("SqlContext")))
            {
                connection.Open();

                // Get the records.
                var recordsSql = @"
                    SELECT
                        P.InAt AS Punch_InAt,
                        P.InAtTimeZone AS Punch_InAtTimeZone,
                        P.LatitudeForInAt AS Punch_LatitudeForInAt,
                        P.LongitudeForInAt AS Punch_LongitudeForInAt,
                        P.LatitudeForOutAt AS Punch_LatitudeForOutAt,
                        P.LongitudeForOutAt AS Punch_LongitudeForOutAt,
                        P.OutAt AS Punch_OutAt,
                        P.OutAtTimeZone AS Punch_OutAtTimeZone,
                        P.InAtSourceHardware AS Punch_InAtSourceHardware,
                        P.InAtSourceHostname AS Punch_InAtSourceHostname,
                        P.OutAtSourceHardware AS Punch_OutAtSourceHardware,
                        P.OutAtSourceHostname AS Punch_OutAtSourceHostname,
                        [J].[Id] AS Job_Id,
                        [J].[CreatedAt] AS Job_CreatedAt,
                        J.CustomerId AS Job_CustomerId,
                        [J].[Name] AS Job_Name,
                        [J].[Number] AS Job_Number,
                        [C].[Id] AS Customer_Id,
                        [C].[CreatedAt] AS Customer_CreatedAt,
                        [C].[Name] AS Customer_Name,
                        [C].[Number] AS Customer_Number,
                        [C].[OrganizationId] AS Customer_OrganizationId,
                        [T].[Id] AS Task_Id,
                        [T].[CreatedAt] AS Task_CreatedAt,
                        T.JobId AS Task_JobId,
                        [T].[Name] AS Task_Name,
                        [T].[Number] AS Task_Number,
                        [U].[Id] AS User_Id,
                        [U].[Name] AS User_Name
                    FROM dbo.Punches AS P
                    JOIN dbo.Tasks AS T ON
                        [T].[Id] = P.TaskId
                    JOIN dbo.Jobs AS J ON
                        [J].[Id] = T.JobId
                    JOIN dbo.Customers AS C ON
                        [C].[Id] = J.CustomerId
                    JOIN dbo.Users AS U ON
                        U.[Id] = P.UserId
                    WHERE
                        [C].[OrganizationId] = @OrganizationId
                        AND [U].[IsActive] = 1
                        AND [U].[IsDeleted] = 0
                        AND
                        (
                            P.OutAt IS NULL
                            OR P.InAt >= @Min
                        )
                    ORDER BY
                        Punch_InAt DESC;";

                var min = new DateTime(DateTime.Today.Year, DateTime.Today.Month, DateTime.Today.Day, 0, 0, 0);
                var results = connection.Query<PunchExpanded>(recordsSql, new { OrganizationId = currentUser.OrganizationId, Min = min });

                foreach (var result in results)
                {
                    punches.Add(new Punch()
                    {
                        InAt = result.Punch_InAt.ToUniversalTime(),
                        InAtTimeZone = result.Punch_InAtTimeZone,
                        LatitudeForInAt = result.Punch_LatitudeForInAt,
                        LongitudeForInAt = result.Punch_LongitudeForInAt,
                        LatitudeForOutAt = result.Punch_LatitudeForOutAt,
                        LongitudeForOutAt = result.Punch_LongitudeForOutAt,
                        OutAt = result.Punch_OutAt.HasValue ? result.Punch_OutAt.Value.ToUniversalTime() : default(DateTime?),
                        OutAtTimeZone = result.Punch_OutAtTimeZone,
                        InAtSourceHardware = result.Punch_InAtSourceHardware,
                        InAtSourceHostname = result.Punch_InAtSourceHostname,
                        OutAtSourceHardware = result.Punch_OutAtSourceHardware,
                        OutAtSourceHostname = result.Punch_OutAtSourceHostname,
                        Task = new Brizbee.Core.Models.Task()
                        {
                            Id = result.Task_Id,
                            CreatedAt = result.Task_CreatedAt,
                            JobId = result.Task_JobId,
                            Name = result.Task_Name,
                            Number = result.Task_Number,
                            QuickBooksPayrollItem = result.Task_QuickBooksPayrollItem,
                            QuickBooksServiceItem = result.Task_QuickBooksServiceItem,
                            BaseServiceRateId = result.Task_BaseServiceRateId,
                            BasePayrollRateId = result.Task_BasePayrollRateId,
                            Job = new Job()
                            {
                                Id = result.Job_Id,
                                CreatedAt = result.Job_CreatedAt,
                                CustomerId = result.Job_CustomerId,
                                Description = result.Job_Description,
                                Name = result.Job_Name,
                                Number = result.Job_Number,
                                QuickBooksCustomerJob = result.Job_QuickBooksCustomerJob,
                                Customer = new Customer()
                                {
                                    Id = result.Customer_Id,
                                    CreatedAt = result.Customer_CreatedAt,
                                    Description = result.Customer_Description,
                                    Name = result.Customer_Name,
                                    Number = result.Customer_Number,
                                    OrganizationId = result.Customer_OrganizationId
                                }
                            }
                        },
                        User = new User()
                        {
                            Id = result.User_Id,
                            CreatedAt = result.User_CreatedAt,
                            EmailAddress = result.User_EmailAddress,
                            IsDeleted = result.User_IsDeleted,
                            Name = result.User_Name,
                            OrganizationId = result.User_OrganizationId,
                            Role = result.User_Role,
                            TimeZone = result.User_TimeZone,
                            UsesMobileClock = result.User_UsesMobileClock,
                            UsesTouchToneClock = result.User_UsesTouchToneClock,
                            UsesWebClock = result.User_UsesWebClock,
                            UsesTimesheets = result.User_UsesTimesheets,
                            RequiresLocation = result.User_RequiresLocation,
                            RequiresPhoto = result.User_RequiresPhoto,
                            QBOGivenName = result.User_QBOGivenName,
                            QBOMiddleName = result.User_QBOMiddleName,
                            QBOFamilyName = result.User_QBOFamilyName,
                            AllowedPhoneNumbers = result.User_AllowedPhoneNumbers,
                            QuickBooksEmployee = result.User_QuickBooksEmployee,
                            QuickBooksVendor = result.User_QuickBooksVendor
                        }
                    });
                }

                connection.Close();
            }

            return Ok(punches);
        }

        // GET: api/Recent/Consumption
        [HttpGet("api/Recent/Consumption")]
        public IActionResult GetRecentConsumption()
        {
            var currentUser = CurrentUser();

            List<QBDInventoryConsumption> consumptions = new List<QBDInventoryConsumption>(0);
            using (var connection = new SqlConnection(_configuration.GetConnectionString("SqlContext")))
            {
                connection.Open();

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
                        C.[OrganizationId] = @OrganizationId
                        AND [U].[IsActive] = 1
                        AND [U].[IsDeleted] = 0
                    ORDER BY
                        C.[CreatedAt] DESC;";

                var results = connection.Query<QBDInventoryConsumptionExpanded>(recordsSql, new { OrganizationId = currentUser.OrganizationId });

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

            return Ok(consumptions);
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