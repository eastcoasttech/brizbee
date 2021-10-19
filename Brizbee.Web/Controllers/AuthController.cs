//
//  AuthController.cs
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

using Brizbee.Common.Models;
using Brizbee.Common.Security;
using Microsoft.ApplicationInsights;
using SendGrid;
using SendGrid.Helpers.Mail;
using Stripe;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.Entity;
using System.Data.Entity.Validation;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Web.Http;

namespace Brizbee.Web.Controllers
{
    public class AuthController : ApiController
    {
        private SqlContext _context = new SqlContext();
        private TelemetryClient telemetryClient = new TelemetryClient();

        // POST: api/Auth/ChangePassword
        [HttpPost]
        [Route("api/Auth/ChangePassword")]
        [AllowAnonymous]
        public IHttpActionResult ChangePassword([FromUri] string emailAddress, [FromUri] string password)
        {
            var user = _context.Users
                .Where(u => !u.IsDeleted)
                .Where(u => u.IsActive == true)
                .Where(u => u.EmailAddress == emailAddress)
                .FirstOrDefault();

            // Generates a password hash and salt.
            var service = new SecurityService();
            user.PasswordSalt = service.GenerateHash(service.GenerateRandomString());
            user.PasswordHash = service.GenerateHash(string.Format("{0} {1}", password, user.PasswordSalt));
            user.Password = null;

            _context.SaveChanges();

            return Ok();
        }

        // POST: api/Auth/Authenticate
        [HttpPost]
        [Route("api/Auth/Authenticate")]
        [AllowAnonymous]
        public IHttpActionResult Authenticate([FromBody] Session session)
        {
            if ("EMAIL" == session.Method.ToUpperInvariant())
            {
                var service = new SecurityService();

                // Validate both an Email and Password.
                if (session.EmailAddress == null || session.EmailPassword == null)
                    return BadRequest("Must provide both an Email address and password.");

                var found = _context.Users
                    .Where(u => u.IsDeleted == false)
                    .Where(u => u.IsActive == true)
                    .Where(u => u.EmailAddress == session.EmailAddress)
                    .Select(u => new
                    {
                        u.Id,
                        u.PasswordHash,
                        u.PasswordSalt
                    })
                    .FirstOrDefault();

                // Attempt to authenticate.
                if ((found == null) || !service.AuthenticateWithPassword(found.PasswordSalt, found.PasswordHash, session.EmailPassword))
                    return BadRequest("Invalid Email address and password combination.");

                return Content(HttpStatusCode.Created, GetCredentials(found.Id));
            }
            else if ("PIN" == session.Method.ToUpperInvariant())
            {
                // Validate both an organization code and user pin.
                if (session.PinUserPin == null || session.PinOrganizationCode == null)
                    return BadRequest("Must provide both an organization code and user pin.");

                var found = _context.Users
                    .Include("Organization")
                    .Where(u => u.IsDeleted == false)
                    .Where(u => u.IsActive == true)
                    .Where(u => u.Organization.Code == session.PinOrganizationCode.ToUpper())
                    .Where(u => u.Pin == session.PinUserPin.ToUpper())
                    .Select(u => new
                    {
                        u.Id
                    })
                    .FirstOrDefault();

                // Attempt to authenticate.
                if (found == null)
                    return BadRequest("Invalid organization code and user pin combination.");


                return Content(HttpStatusCode.Created, GetCredentials(found.Id));
            }
            else
            {
                return BadRequest("Must authenticate via either Email or pin method.");
            }
        }

        // POST: api/Auth/Register
        [HttpPost]
        [Route("api/Auth/Register")]
        [AllowAnonymous]
        public IHttpActionResult Register([FromBody] Registration registration)
        {
            telemetryClient.TrackEvent("Registration:Requested");

            var user = registration.User;
            var organization = registration.Organization;

            using (var transaction = _context.Database.BeginTransaction())
            {
                try
                {
                    // Ensure Email address is unique.
                    var duplicate = _context.Users.Where(u => u.EmailAddress.ToLower().Equals(user.EmailAddress));
                    if (duplicate.Any())
                        return BadRequest("Email address is already taken.");

                    // Generate an organization code if one is not provided.
                    var code = organization.Code;
                    while (string.IsNullOrEmpty(code))
                    {
                        var generated = GetRandomNumber();

                        var found = _context.Organizations
                            .Where(o => o.Code == generated);

                        if (!found.Any())
                            code = generated;
                    }
                    organization.Code = code;

                    // Generate a user pin if one is not provided.
                    if (string.IsNullOrEmpty(user.Pin))
                        user.Pin = GetRandomNumber();

                    // Generates a password hash and salt.
                    var service = new SecurityService();
                    user.PasswordSalt = service.GenerateHash(service.GenerateRandomString());
                    user.PasswordHash = service.GenerateHash(string.Format("{0} {1}", user.Password, user.PasswordSalt));
                    user.Password = null;

                    // Formatting
                    user.EmailAddress = user.EmailAddress.ToLower();

                    // Auto-generated.
                    user.Role = "Administrator";
                    user.CreatedAt = DateTime.UtcNow;
                    user.AllowedPhoneNumbers = "*";
                    user.TimeZone = "America/New_York";
                    user.IsActive = true;
                    user.CanViewPunches = true;
                    user.CanCreatePunches = true;
                    user.CanModifyPunches = true;
                    user.CanDeletePunches = true;
                    user.CanSplitAndPopulatePunches = true;
                    user.CanViewReports = true;
                    user.CanViewLocks = true;
                    user.CanCreateLocks = true;
                    user.CanUndoLocks = true;
                    user.CanViewTimecards = true;
                    user.CanCreateTimecards = true;
                    user.CanModifyTimecards = true;
                    user.CanDeleteTimecards = true;
                    user.CanViewUsers = true;
                    user.CanCreateUsers = true;
                    user.CanModifyUsers = true;
                    user.CanDeleteUsers = true;
                    user.CanViewInventoryItems = true;
                    user.CanSyncInventoryItems = true;
                    user.CanViewInventoryConsumptions = true;
                    user.CanSyncInventoryConsumptions = true;
                    user.CanDeleteInventoryConsumptions = true;
                    user.CanViewRates = true;
                    user.CanCreateRates = true;
                    user.CanModifyRates = true;
                    user.CanDeleteRates = true;
                    user.CanViewOrganizationDetails = true;
                    user.CanModifyOrganizationDetails = true;
                    user.CanViewCustomers = true;
                    user.CanCreateCustomers = true;
                    user.CanModifyCustomers = true;
                    user.CanDeleteCustomers = true;
                    user.CanViewProjects = true;
                    user.CanCreateProjects = true;
                    user.CanModifyProjects = true;
                    user.CanDeleteProjects = true;
                    user.CanViewTasks = true;
                    user.CanCreateTasks = true;
                    user.CanModifyTasks = true;
                    user.CanDeleteTasks = true;
                    organization.CreatedAt = DateTime.UtcNow;
                    organization.MinutesFormat = "minutes";
                    organization.StripeCustomerId = "UNSPECIFIED";
                    organization.StripeSubscriptionId = "UNSPECIFIED";
                    organization.ShowCustomerNumber = true;
                    organization.ShowProjectNumber = true;
                    organization.ShowTaskNumber = true;
                    organization.SortCustomersByColumn = "Number";
                    organization.SortProjectsByColumn = "Number";
                    organization.SortTasksByColumn = "Number";

                    // Determine the actual Stripe Plan Id based on the PlanId.
                    var stripePlanId = ConfigurationManager.AppSettings["StripePlanId1"].ToString(); // Default plan is the contractor plan
                    switch (organization.PlanId)
                    {
                        case 1:
                            stripePlanId = ConfigurationManager.AppSettings["StripePlanId1"].ToString();
                            break;
                        case 2:
                            stripePlanId = ConfigurationManager.AppSettings["StripePlanId2"].ToString();
                            break;
                        case 3:
                            stripePlanId = ConfigurationManager.AppSettings["StripePlanId3"].ToString();
                            break;
                        case 4:
                            stripePlanId = ConfigurationManager.AppSettings["StripePlanId4"].ToString();
                            break;
                    }

                    if (!string.IsNullOrEmpty(stripePlanId))
                    {
                        try
                        {
                            telemetryClient.TrackEvent("Registration:Subscribe");

                            // Create a Stripe customer object and save the customer id.
                            var customerOptions = new CustomerCreateOptions
                            {
                                Email = user.EmailAddress
                            };
                            var customers = new CustomerService();
                            Stripe.Customer customer = customers.Create(customerOptions);
                            organization.StripeCustomerId = customer.Id;

                            // Subscribe the customer to the price and save the subscription id.
                            var subscriptionOptions = new SubscriptionCreateOptions
                            {
                                Customer = customer.Id, // ex. cus_IDjvN9UsoFp2mk
                                Items = new List<SubscriptionItemOptions>
                            {
                                new SubscriptionItemOptions
                                {
                                    Price = stripePlanId // ex. price_1Hd5PvLI44a19MHX9w5EGD4r
                                }
                            },
                                TrialFromPlan = true
                            };
                            var subscriptions = new SubscriptionService();
                            Subscription subscription = subscriptions.Create(subscriptionOptions);

                            organization.StripeSubscriptionId = subscription.Id;

                            // Send Welcome Email.
                            telemetryClient.TrackEvent("Registration:Email");

                            var apiKey = ConfigurationManager.AppSettings["SendGridApiKey"].ToString();
                            var client = new SendGridClient(apiKey);
                            var from = new EmailAddress("BRIZBEE <administrator@brizbee.com>");
                            var to = new EmailAddress(user.EmailAddress);
                            var msg = MailHelper.CreateSingleTemplateEmail(from, to, "d-8c48a9ad2ddd4d73b6e6c10307182f43", null);
                            var response = client.SendEmailAsync(msg);
                        }
                        catch (Exception ex)
                        {
                            telemetryClient.TrackException(ex);
                            return BadRequest(ex.Message);
                        }
                    }

                    // Save the organization and user.
                    _context.Organizations.Add(organization);
                    user.OrganizationId = organization.Id;

                    _context.Users.Add(user);

                    _context.SaveChanges();

                    telemetryClient.TrackEvent("Registration:Succeeded");

                    transaction.Commit();

                    return Created("auth/me", user);
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

                    telemetryClient.TrackException(ex);

                    return BadRequest(message);
                }
                catch (Exception ex)
                {
                    transaction.Rollback();

                    telemetryClient.TrackException(ex);

                    return BadRequest(ex.Message);
                }
            }
        }

        // GET: api/Auth/Me
        [HttpGet]
        [Route("api/Auth/Me")]
        public IHttpActionResult Me()
        {
            var currentUser = CurrentUser();

            if (currentUser == null) return BadRequest();

            var user = _context.Users
                .Include(u => u.Organization)
                .Where(u => u.Id == currentUser.Id)
                .Select(u => new
                {
                    u.AllowedPhoneNumbers,
                    u.CanCreateCustomers,
                    u.CanCreateLocks,
                    u.CanCreateProjects,
                    u.CanCreatePunches,
                    u.CanCreateRates,
                    u.CanCreateTasks,
                    u.CanCreateTimecards,
                    u.CanCreateUsers,
                    u.CanDeleteCustomers,
                    u.CanDeleteInventoryConsumptions,
                    u.CanDeleteProjects,
                    u.CanDeletePunches,
                    u.CanDeleteRates,
                    u.CanDeleteTasks,
                    u.CanDeleteTimecards,
                    u.CanDeleteUsers,
                    u.CanModifyCustomers,
                    u.CanModifyInventoryItems,
                    u.CanModifyOrganizationDetails,
                    u.CanModifyProjects,
                    u.CanModifyPunches,
                    u.CanModifyRates,
                    u.CanModifyTasks,
                    u.CanModifyTimecards,
                    u.CanModifyUsers,
                    u.CanSplitAndPopulatePunches,
                    u.CanSyncInventoryConsumptions,
                    u.CanSyncInventoryItems,
                    u.CanUndoLocks,
                    u.CanViewCustomers,
                    u.CanViewInventoryConsumptions,
                    u.CanViewInventoryItems,
                    u.CanViewLocks,
                    u.CanViewOrganizationDetails,
                    u.CanViewProjects,
                    u.CanViewPunches,
                    u.CanViewRates,
                    u.CanViewReports,
                    u.CanViewTasks,
                    u.CanViewTimecards,
                    u.CanViewUsers,
                    u.CreatedAt,
                    u.EmailAddress,
                    u.Id,
                    u.IsActive,
                    u.IsDeleted,
                    u.Name,
                    u.NotificationMobileNumbers,
                    u.OrganizationId,
                    u.Pin,
                    u.QuickBooksEmployee,
                    u.RequiresLocation,
                    u.RequiresPhoto,
                    u.Role,
                    u.TimeZone,
                    u.UsesMobileClock,
                    u.UsesTimesheets,
                    u.UsesTouchToneClock,
                    u.UsesWebClock,
                    Organization = new
                    {
                        u.Organization.Code,
                        u.Organization.CreatedAt,
                        u.Organization.Groups,
                        u.Organization.Id,
                        u.Organization.MinutesFormat,
                        u.Organization.Name,
                        u.Organization.PlanId
                    }
                })
                .FirstOrDefault();

            return Ok(user);
        }

        private Credential GetCredentials(int userId)
        {
            // Return a token, expiration, and user id. The
            // token and expiration are concatenated and verified
            // later as part of each request.
            var credential = new Credential();

            var service = new SecurityService();

            var now = DateTime.UtcNow.AddDays(1).Ticks.ToString();
            var secretKey = ConfigurationManager.AppSettings["AuthenticationSecretKey"];
            var token = string.Format("{0} {1} {2}", secretKey, userId.ToString(), now);
            credential.AuthToken = service.GenerateHash(token);
            credential.AuthExpiration = now;
            credential.AuthUserId = userId.ToString();

            return credential;
        }

        private string GetRandomNumber()
        {
            var code = "";

            for (int i = 1; i <= 5; i++)
            {
                Random rnd = new Random();
                code += rnd.Next(0, 9).ToString();
            }

            return code;
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
    }
}
