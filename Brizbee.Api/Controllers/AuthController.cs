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

using Brizbee.Api.Services;
using Brizbee.Core.Models;
using Brizbee.Core.Security;
using Microsoft.ApplicationInsights;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using RestSharp;
using RestSharp.Authenticators;
using Stripe;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace Brizbee.Api.Controllers
{
    public class AuthController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        private readonly SqlContext _context;
        private readonly TelemetryClient _telemetryClient;

        public AuthController(IConfiguration configuration, SqlContext context, TelemetryClient telemetryClient)
        {
            _configuration = configuration;
            _context = context;
            _telemetryClient = telemetryClient;
        }

        // POST: api/Auth/ChangePassword
        [HttpPost("api/Auth/ChangePassword")]
        [AllowAnonymous]
        public IActionResult ChangePassword([FromQuery] string emailAddress, [FromQuery] string password)
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
        [HttpPost("api/Auth/Authenticate")]
        [AllowAnonymous]
        public IActionResult Authenticate([FromBody] Session session)
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
                        u.EmailAddress,
                        u.PasswordSalt,
                        u.PasswordHash
                    })
                    .FirstOrDefault();

                // Attempt to authenticate.
                if ((found == null) || !service.AuthenticateWithPassword(found.PasswordSalt, found.PasswordHash, session.EmailPassword))
                    return BadRequest("Invalid Email address and password combination.");

                return Created("auth/authenticate", new
                {
                    Token = GenerateJSONWebToken(found.Id, found.EmailAddress)
                });
            }
            else if ("PIN" == session.Method.ToUpperInvariant())
            {
                // Validate both an organization code and user pin.
                if (session.PinUserPin == null || session.PinOrganizationCode == null)
                    return BadRequest("Must provide both an organization code and user pin.");

                var found = _context.Users
                    .Include(u => u.Organization)
                    .Where(u => u.IsDeleted == false)
                    .Where(u => u.IsActive == true)
                    .Where(u => u.Organization.Code == session.PinOrganizationCode.ToUpper())
                    .Where(u => u.Pin == session.PinUserPin.ToUpper())
                    .Select(u => new
                    {
                        u.Id,
                        u.EmailAddress
                    })
                    .FirstOrDefault();

                // Attempt to authenticate.
                if (found == null)
                    return BadRequest("Invalid organization code and user pin combination.");

                return Created("auth/authenticate", new
                {
                    Token = GenerateJSONWebToken(found.Id, found.EmailAddress)
                });
            }
            else
            {
                return BadRequest("Must authenticate via either Email or pin method.");
            }
        }

        // POST: api/Auth/Register
        [HttpPost("api/Auth/Register")]
        [AllowAnonymous]
        public async Task<IActionResult> Register([FromBody] Registration registration)
        {
            _telemetryClient.TrackEvent("Registration:Requested");

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
                    var stripePlanId = _configuration.GetSection("Stripe").GetValue<string>("StripePlanId1"); // Default plan is the contractor plan
                    switch (organization.PlanId)
                    {
                        case 1:
                            stripePlanId = _configuration.GetSection("Stripe").GetValue<string>("StripePlanId1");
                            break;
                        case 2:
                            stripePlanId = _configuration.GetSection("Stripe").GetValue<string>("StripePlanId2");
                            break;
                        case 3:
                            stripePlanId = _configuration.GetSection("Stripe").GetValue<string>("StripePlanId3");
                            break;
                        case 4:
                            stripePlanId = _configuration.GetSection("Stripe").GetValue<string>("StripePlanId4");
                            break;
                    }

                    if (!string.IsNullOrEmpty(stripePlanId))
                    {
                        try
                        {
                            _telemetryClient.TrackEvent("Registration:Subscribe");

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

                            try
                            {
                                // Prepare to send the welcome Email.
                                _telemetryClient.TrackEvent("Registration:Email");

                                var apiKey = _configuration.GetValue<string>("MailgunApiKey");

                                var options = new RestClientOptions("https://api.mailgun.net")
                                {
                                    Authenticator = new HttpBasicAuthenticator("api", apiKey ?? "API_KEY")
                                };

                                var client = new RestClient(options);

                                var request = new RestRequest("/v3/mg.brizbee.com/messages", Method.Post);

                                request.AlwaysMultipartFormData = true;

                                request.AddParameter("from", "Brizbee <postmaster@mg.brizbee.com>");
                                request.AddParameter("to", $"{user.Name} <{user.EmailAddress}>");
                                request.AddParameter("subject", "Welcome to Brizbee");
                                request.AddParameter("template", "welcome");

                                // Send the Email.
                                await client.ExecuteAsync(request);
                            }
                            catch (Exception ex)
                            {
                                _telemetryClient.TrackException(ex);
                            }
                        }
                        catch (Exception ex)
                        {
                            _telemetryClient.TrackException(ex);
                            return BadRequest(ex.Message);
                        }
                    }

                    // Save the organization and user.
                    _context.Organizations.Add(organization);
                    _context.SaveChanges();

                    user.OrganizationId = organization.Id;

                    _context.Users.Add(user);
                    _context.SaveChanges();

                    _telemetryClient.TrackEvent("Registration:Succeeded");

                    transaction.Commit();

                    return Created("auth/me", user);
                }
                catch (DbUpdateException ex)
                {
                    transaction.Rollback();

                    _telemetryClient.TrackException(ex);

                    return BadRequest(ex.Message);
                }
                catch (Exception ex)
                {
                    transaction.Rollback();

                    _telemetryClient.TrackException(ex);

                    return BadRequest(ex.Message);
                }
            }
        }

        // GET: api/Auth/Me
        [HttpGet("api/Auth/Me")]
        public IActionResult Me()
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
                    u.CanMergeProjects,
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
                    u.ShouldSendMidnightPunchEmail,
                    Organization = new
                    {
                        u.Organization.Code,
                        u.Organization.CreatedAt,
                        u.Organization.Groups,
                        u.Organization.Id,
                        u.Organization.MinutesFormat,
                        u.Organization.Name,
                        u.Organization.PlanId,
                        u.Organization.ShowCustomerNumber,
                        u.Organization.ShowProjectNumber,
                        u.Organization.ShowTaskNumber,
                        u.Organization.SortCustomersByColumn,
                        u.Organization.SortProjectsByColumn,
                        u.Organization.SortTasksByColumn
                    }
                })
                .FirstOrDefault();

            return Ok(user);
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

        private string GenerateJSONWebToken(int userId, string? emailAddress)
        {
            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]));
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

            var claims = new[] {
                new Claim(JwtRegisteredClaimNames.Sub, userId.ToString()),
                new Claim(JwtRegisteredClaimNames.Email, string.IsNullOrEmpty(emailAddress) ? "" : emailAddress),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };

            var token = new JwtSecurityToken(_configuration["Jwt:Issuer"],
                _configuration["Jwt:Issuer"],
                claims,
                expires: DateTime.UtcNow.AddMinutes(120),
                signingCredentials: credentials);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        private User CurrentUser()
        {
            var type = "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier";
            var sub = HttpContext.User.Claims.FirstOrDefault(c => c.Type == type).Value;
            var currentUserId = int.Parse(sub);
            return _context.Users
                .Include(u => u.Organization)
                .Where(u => u.Id == currentUserId)
                .FirstOrDefault();
        }
    }
}
