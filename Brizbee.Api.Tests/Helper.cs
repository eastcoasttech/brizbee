//
//  Helper.cs
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
using Dapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System;
using System.Diagnostics;
using System.Linq;

namespace Brizbee.Api.Tests
{
    public class Helper
    {
        public IConfiguration _configuration { get; set; }
        public SqlContext _context { get; set; }

        public Helper()
        {
            // Setup configuration
            var configurationBuilder = new ConfigurationBuilder();
            configurationBuilder.AddJsonFile("appsettings.json");
            _configuration = configurationBuilder.Build();

            // Setup database context
            var options = new DbContextOptionsBuilder<SqlContext>()
                .UseSqlServer(_configuration.GetConnectionString("SqlContext"))
                .Options;
            _context = new SqlContext(options);
        }

        public void Prepare()
        {
            try
            {
                // ----------------------------------------------------------------
                // Scaffold an organization.
                // ----------------------------------------------------------------

                var organization = new Organization()
                {
                    CreatedAt = DateTime.UtcNow,
                    Name = "Test Organization",
                    Code = "1234",
                    StripeCustomerId = "BLANK",
                    StripeSubscriptionId = "BLANK",
                    SortCustomersByColumn = "Number",
                    SortProjectsByColumn = "Number",
                    SortTasksByColumn = "Number",
                    ShowCustomerNumber = true,
                    ShowProjectNumber = true,
                    ShowTaskNumber = true
                };
                _context.Organizations.Add(organization);
                _context.SaveChanges();

                // ----------------------------------------------------------------
                // Scaffold a user.
                // ----------------------------------------------------------------

                var user = new User()
                {
                    CreatedAt = DateTime.UtcNow,
                    EmailAddress = "test.user.a@brizbee.com",
                    Name = "Test User A",
                    OrganizationId = organization.Id,
                    IsDeleted = false,
                    IsActive = true,
                    Pin = "0000",
                    Role = "Standard",
                    TimeZone = "America/New_York",
                    AllowedPhoneNumbers = "*",
                    UsesTouchToneClock = true,
                    CanCreateUsers = true,
                    CanDeleteUsers = true,
                    CanModifyUsers = true,
                    CanViewUsers = true,
                    CanCreateCustomers = true,
                    CanDeleteCustomers = true,
                    CanModifyCustomers = true,
                    CanViewCustomers = true,
                    CanCreateProjects = true,
                    CanDeleteProjects = true,
                    CanModifyProjects = true,
                    CanViewProjects = true,
                    CanCreateTasks = true,
                    CanDeleteTasks = true,
                    CanModifyTasks = true,
                    CanViewTasks = true,
                    CanCreateRates = true,
                    CanDeleteRates = true,
                    CanModifyRates = true,
                    CanViewRates = true,
                    CanCreateLocks = true,
                    CanUndoLocks = true,
                    CanViewLocks = true,
                    CanCreatePunches = true,
                    CanDeletePunches = true,
                    CanModifyPunches = true,
                    CanViewPunches = true
                };
                _context.Users.Add(user);
                _context.SaveChanges();


                // ----------------------------------------------------------------
                // Scaffold a customer.
                // ----------------------------------------------------------------

                var customer = new Customer()
                {
                    CreatedAt = DateTime.UtcNow,
                    Number = "1000",
                    Name = "General Electric",
                    OrganizationId = organization.Id
                };
                _context.Customers.Add(customer);
                _context.SaveChanges();


                // ----------------------------------------------------------------
                // Scaffold a project.
                // ----------------------------------------------------------------

                var customerId = _context.Customers
                    .Where(c => c.Name == "General Electric")
                    .Select(c => c.Id)
                    .FirstOrDefault();
                var project = new Job()
                {
                    CreatedAt = DateTime.UtcNow,
                    Number = "1000",
                    Name = "Install Motor",
                    QuickBooksCustomerJob = "",
                    QuickBooksClass = "",
                    CustomerId = customerId,
                    Status = "Open"
                };
                _context.Jobs.Add(project);
                _context.SaveChanges();


                // ----------------------------------------------------------------
                // Scaffold a task.
                // ----------------------------------------------------------------

                var jobId = _context.Jobs
                    .Where(j => j.Name == "Install Motor")
                    .Select(j => j.Id)
                    .FirstOrDefault();
                var task = new Task()
                {
                    CreatedAt = DateTime.UtcNow,
                    Number = "1000",
                    Name = "Installation",
                    JobId = jobId
                };
                _context.Tasks.Add(task);
                _context.SaveChanges();


                // ----------------------------------------------------------------
                // Scaffold a punch.
                // ----------------------------------------------------------------

                var punch = new Punch()
                {
                    CreatedAt = DateTime.UtcNow,
                    TaskId = task.Id,
                    InAt = new DateTime(2022, 1, 1, 8, 0, 0),
                    OutAt = new DateTime(2022, 1, 1, 17, 0, 0),
                    Guid = Guid.NewGuid(),
                    UserId = user.Id
                };
                _context.Punches.Add(punch);
                _context.SaveChanges();


                // ----------------------------------------------------------------
                // Scaffold an inventory item sync.
                // ----------------------------------------------------------------

                var inventoryItemSync = new QBDInventoryItemSync()
                {
                    OrganizationId = organization.Id,
                    CreatedAt = DateTime.UtcNow,
                    CreatedByUserId = user.Id,
                    Hostname = "HOSTNAME-01",
                    HostCompanyFileName = "COMPANY FILE NAME",
                    HostCompanyFilePath = "PATH TO FILE",
                    HostCountry = "US",
                    HostMajorVersion = "1",
                    HostMinorVersion = "0",
                    HostProductName = "PRODUCT NAME",
                    HostSupportedQBXMLVersion = "12"
                };
                _context.QBDInventoryItemSyncs.Add(inventoryItemSync);
                _context.SaveChanges();


                // ----------------------------------------------------------------
                // Scaffold an inventory item.
                // ----------------------------------------------------------------

                var inventoryItem = new QBDInventoryItem()
                {
                    ListId = "019F79CACE27HDKDUD32",
                    Name = "25-ft 12/2 Solid NM",
                    FullName = "25-ft 12/2 Solid NM",
                    PurchaseCost = 38.49m,
                    PurchaseDescription = "Romex SIMpull 25-ft 12/2 Solid Non-Metallic Wire (By-the-Roll)",
                    SalesPrice = 48.11m,
                    SalesDescription = "Romex SIMpull 25-ft 12/2 Solid Non-Metallic Wire (By-the-Roll)",
                    BarCodeValue = "70012",
                    CustomBarCodeValue = "70012",
                    OrganizationId = organization.Id,
                    ManufacturerPartNumber = "28828221",
                    QBDInventoryItemSyncId = inventoryItemSync.Id
                };
                _context.QBDInventoryItems.Add(inventoryItem);
                _context.SaveChanges();
            }
            catch (DbUpdateException ex)
            {
                Trace.TraceError(ex.ToString());
            }
            catch (Exception ex)
            {
                Trace.TraceError(ex.ToString());
            }
        }

        public void Cleanup()
        {
            _context.Database.GetDbConnection().Query("DELETE FROM [dbo].[Punches]");
            _context.Database.GetDbConnection().Query("DELETE FROM [dbo].[Commits]");
            _context.Database.GetDbConnection().Query("DELETE FROM [dbo].[TimesheetEntries]");
            _context.Database.GetDbConnection().Query("DELETE FROM [dbo].[Tasks]");
            _context.Database.GetDbConnection().Query("DELETE FROM [dbo].[Jobs]");
            _context.Database.GetDbConnection().Query("DELETE FROM [dbo].[Customers]");
            _context.Database.GetDbConnection().Query("DELETE FROM [dbo].[Users]");
            _context.Database.GetDbConnection().Query("DELETE FROM [dbo].[Organizations]");
        }
    }
}
