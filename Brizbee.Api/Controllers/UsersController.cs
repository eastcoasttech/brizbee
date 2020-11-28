//
//  UsersController.cs
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
using Brizbee.Common.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using SendGrid;
using SendGrid.Helpers.Mail;
using Stripe;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace Brizbee.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class UsersController : ControllerBase
    {
        private readonly IConfiguration _config;
        private readonly SqlContext _context;
        private readonly IWebHostEnvironment _env;

        public UsersController(IConfiguration configuration, SqlContext context, IWebHostEnvironment env)
        {
            _config = configuration;
            _context = context;
            _env = env;
        }

        // GET: api/Users
        [HttpGet]
        public async Task<ActionResult<IEnumerable<User>>> GetUsers()
        {
            var currentUser = CurrentUser();

            // Only permit administrators to see all users
            if (currentUser.Role != "Administrator")
            {
                return BadRequest();
            }

            return await _context.Users
                .Where(u => !u.IsDeleted)
                .Where(u => u.OrganizationId == currentUser.OrganizationId)
                .ToListAsync();
        }

        // GET: api/Users/5
        [HttpGet("{id}")]
        public async Task<ActionResult<User>> GetUser(int id)
        {
            var currentUser = CurrentUser();

            // Only Administrators can see other users in the organization
            if (currentUser.Role != "Administrator" && currentUser.Id != id)
            {
                return BadRequest();
            }

            // Search within the organization
            var user = await _context.Users
                .Where(u => !u.IsDeleted)
                .Where(u => u.OrganizationId == currentUser.OrganizationId)
                .Where(u => u.Id == id)
                .FirstOrDefaultAsync();

            if (user == null)
            {
                return NotFound();
            }

            return user;
        }

        // POST: api/Users
        [HttpPost]
        public async Task<ActionResult<User>> PostUser(User user)
        {
            var currentUser = CurrentUser();

            // Ensure the same organization
            user.OrganizationId = currentUser.OrganizationId;

            // Only permit administrators
            if (currentUser.Role != "Administrator")
            {
                return BadRequest();
            }

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            return CreatedAtAction("GetUser", new { id = user.Id }, user);
        }

        // PUT: api/Users/5
        [HttpPut("{id}")]
        public IActionResult PutUser(int id, User patch)
        {
            var currentUser = CurrentUser();

            // Search within the organization
            var user = _context.Users
                .Where(u => !u.IsDeleted)
                .Where(u => u.OrganizationId == currentUser.OrganizationId)
                .Where(u => u.Id == id)
                .FirstOrDefault();

            if (user == null)
            {
                return NotFound();
            }

            // Only permit administrators and the same user to update
            if (currentUser.Role != "Administrator" && currentUser.Id != id)
            {
                return BadRequest();
            }

            // Apply the changes
            user.Name = patch.Name;
            user.EmailAddress = patch.EmailAddress;
            user.Pin = patch.Pin;
            user.QBOFamilyName = patch.QBOFamilyName;
            user.QBOGivenName = patch.QBOGivenName;
            user.QBOMiddleName = patch.QBOMiddleName;
            user.QuickBooksEmployee = patch.QuickBooksEmployee;
            user.RequiresLocation = patch.RequiresLocation;
            user.RequiresPhoto = patch.RequiresPhoto;
            user.Role = patch.Role;
            user.TimeZone = patch.TimeZone;
            user.UsesMobileClock = patch.UsesMobileClock;
            user.UsesTimesheets = patch.UsesTimesheets;
            user.UsesTouchToneClock = patch.UsesTouchToneClock;
            user.UsesWebClock = patch.UsesWebClock;

            try
            {
                _context.SaveChanges();
            }
            catch (DbUpdateConcurrencyException)
            {
                throw;
            }

            return NoContent();
        }

        // DELETE: api/Users/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteUser(int id)
        {
            var currentUser = CurrentUser();

            // Only permit administrators to delete users
            if (currentUser.Role != "Administrator")
            {
                return BadRequest();
            }

            // Search within the organization
            var user = await _context.Users
                .Where(u => !u.IsDeleted)
                .Where(u => u.OrganizationId == currentUser.OrganizationId)
                .Where(u => u.Id == id)
                .FirstOrDefaultAsync();

            if (user == null)
            {
                return NotFound();
            }

            // Apply the changes
            user.IsDeleted = true;

            await _context.SaveChangesAsync();

            return NoContent();
        }

        // POST: api/Users/ChangePassword
        [HttpPost]
        [AllowAnonymous]
        [Route("ChangePassword")]
        public IActionResult ChangePassword([FromQuery] string emailAddress, [FromQuery] string password)
        {
            var user = _context.Users
                .Where(u => !u.IsDeleted)
                .Where(u => u.EmailAddress == emailAddress)
                .FirstOrDefault();

            // Generates a password hash and salt
            var service = new SecurityService();
            user.PasswordSalt = service.GenerateHash(service.GenerateRandomString());
            user.PasswordHash = service.GenerateHash(string.Format("{0} {1}", password, user.PasswordSalt));
            user.Password = null;

            _context.SaveChanges();

            return Ok();
        }

        // POST: api/Users/Authenticate
        [HttpPost]
        [AllowAnonymous]
        [Route("Authenticate")]
        public IActionResult Authenticate([FromBody] Session session)
        {
            User user = null;

            switch (session.Method)
            {
                case "email":
                    var service = new SecurityService();

                    // Validate both an Email and Password
                    if (session.EmailAddress == null || session.EmailPassword == null)
                    {
                        return BadRequest("Must provide both an Email Address and password");
                    }

                    user = _context.Users
                        .Where(u => u.EmailAddress == session.EmailAddress)
                        .FirstOrDefault();

                    // Attempt to authenticate
                    if ((user == null) ||
                        !service.AuthenticateWithPassword(user,
                            session.EmailPassword))
                    {
                        return BadRequest("Invalid Email Address and password combination");
                    }

                    return Created("Authenticate", GenerateJSONWebToken(user));
                case "pin":
                    // Validate both an organization code and user pin
                    if (session.PinUserPin == null || session.PinOrganizationCode == null)
                    {
                        return BadRequest("Must provide both an organization code and user PIN");
                    }

                    user = _context.Users
                        .Include("Organization")
                        .Where(u => u.Pin == session.PinUserPin.ToUpper())
                        .Where(u => u.Organization.Code ==
                            session.PinOrganizationCode.ToUpper())
                        .FirstOrDefault();

                    // Attempt to authenticate
                    if (user == null)
                    {
                        return BadRequest("Invalid organization code and user pin combination");
                    }

                    return Created("Authenticate", GenerateJSONWebToken(user));
                default:
                    return BadRequest();
            }
        }

        // POST: api/Users/Register
        [HttpPost]
        [AllowAnonymous]
        [Route("Register")]
        public IActionResult Register([FromBody] Registration registration)
        {
            var user = registration.User;
            var organization = registration.Organization;

            using (var transaction = _context.Database.BeginTransaction())
            {
                try
                {
                    // Ensure Email address is unique
                    var duplicate = _context.Users.Where(u => u.EmailAddress.ToLower().Equals(user.EmailAddress));
                    if (duplicate.Any())
                    {
                        return BadRequest("Email Address is already taken");
                    }

                    // Generates a password hash and salt
                    var service = new SecurityService();
                    user.PasswordSalt = service.GenerateHash(service.GenerateRandomString());
                    user.PasswordHash = service.GenerateHash(string.Format("{0} {1}", user.Password, user.PasswordSalt));
                    user.Password = null;

                    // Auto-generated
                    user.Role = "Administrator";
                    user.CreatedAt = DateTime.UtcNow;
                    organization.CreatedAt = DateTime.UtcNow;
                    organization.MinutesFormat = "minutes";

                    if (_env.EnvironmentName.ToUpperInvariant() == "DEVELOPMENT")
                    {
                        organization.StripeCustomerId = string.Format("RANDOM{0}", new SecurityService().GenerateRandomString());
                        organization.StripeSubscriptionId = string.Format("RANDOM{0}", new SecurityService().GenerateRandomString());
                    }

                    // Determine the actual Stripe Plan Id based on the PlanId
                    var stripePlanId = _config["Stripe:StripePlanId1"]; // Default plan is the contractor plan
                    switch (organization.PlanId)
                    {
                        case 1:
                            stripePlanId = _config["Stripe:StripePlanId1"];
                            break;
                        case 2:
                            stripePlanId = _config["Stripe:StripePlanId2"];
                            break;
                        case 3:
                            stripePlanId = _config["Stripe:StripePlanId3"];
                            break;
                        case 4:
                            stripePlanId = _config["Stripe:StripePlanId4"];
                            break;
                    }

                    if (_env.EnvironmentName.ToUpperInvariant() != "DEVELOPMENT")
                    {
                        try
                        {
                            // Create a Stripe customer object and save the customer id
                            var customerOptions = new CustomerCreateOptions
                            {
                                Email = user.EmailAddress
                            };
                            var customers = new CustomerService();
                            Stripe.Customer customer = customers.Create(customerOptions);
                            organization.StripeCustomerId = customer.Id;

                            // Subscribe the customer to the price and save the subscription id
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

                            // Send Welcome Email
                            var apiKey = _config["SendGrid:ApiKey"];
                            var client = new SendGridClient(apiKey);
                            var from = new EmailAddress("BRIZBEE <administrator@brizbee.com>");
                            var to = new EmailAddress(user.EmailAddress);
                            var msg = MailHelper.CreateSingleTemplateEmail(from, to, "d-8c48a9ad2ddd4d73b6e6c10307182f43", null);
                            var response = client.SendEmailAsync(msg);
                        }
                        catch (Exception ex)
                        {
                            return BadRequest(ex.Message);
                        }
                    }

                    // Save the organization and user
                    _context.Organizations.Add(organization);
                    user.OrganizationId = organization.Id;

                    _context.Users.Add(user);

                    _context.SaveChanges();

                    transaction.Commit();

                    return Created($"users/{user.Id}", user);
                }
                catch (Exception ex)
                {
                    transaction.Rollback();

                    return BadRequest(ex.Message);
                }
            }
        }

        private string GenerateJSONWebToken(User user)
        {
            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"]));
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

            var claims = new[] {
                new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
                new Claim(JwtRegisteredClaimNames.Email, user.EmailAddress),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };

            var token = new JwtSecurityToken(_config["Jwt:Issuer"],
                _config["Jwt:Issuer"],
                claims,
                expires: DateTime.Now.AddMinutes(120),
                signingCredentials: credentials);

            return new JwtSecurityTokenHandler().WriteToken(token);
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
