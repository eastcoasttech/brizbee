﻿//
//  Helper.cs
//  BRIZBEE API
//
//  Copyright (C) 2021 East Coast Technology Services, LLC
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
using Dapper;
using System;
using System.Data.Entity.Validation;
using System.Diagnostics;
using System.Linq;

namespace Brizbee.Web.Tests
{
    public class Helper
    {
        private readonly SqlContext _context = new SqlContext();

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

                var organizationId = _context.Organizations
                    .Where(o => o.Name == "Test Organization")
                    .Select(o => o.Id)
                    .FirstOrDefault();
                var user = new User()
                {
                    CreatedAt = DateTime.UtcNow,
                    EmailAddress = "test.user.a@brizbee.com",
                    Name = "Test User A",
                    OrganizationId = organizationId,
                    IsDeleted = false,
                    Pin = "0000",
                    Role = "Standard",
                    TimeZone = "America/New_York",
                    AllowedPhoneNumbers = ""
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
                    OrganizationId = organizationId
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
            }
            catch (DbEntityValidationException e)
            {
                string message = "";

                foreach (var eve in e.EntityValidationErrors)
                {
                    foreach (var ve in eve.ValidationErrors)
                    {
                        message += string.Format("{0} has error '{1}'; ", ve.PropertyName, ve.ErrorMessage);
                    }
                }

                Trace.TraceError(message);
            }
            catch (Exception ex)
            {
                Trace.TraceError(ex.ToString());
            }
        }

        public void Cleanup()
        {
            _context.Database.Connection.Query("DELETE FROM [dbo].[Punches]");
            _context.Database.Connection.Query("DELETE FROM [dbo].[TimesheetEntries]");
            _context.Database.Connection.Query("DELETE FROM [dbo].[Tasks]");
            _context.Database.Connection.Query("DELETE FROM [dbo].[Jobs]");
            _context.Database.Connection.Query("DELETE FROM [dbo].[Customers]");
            _context.Database.Connection.Query("DELETE FROM [dbo].[Users]");
            _context.Database.Connection.Query("DELETE FROM [dbo].[Organizations]");
        }
    }
}