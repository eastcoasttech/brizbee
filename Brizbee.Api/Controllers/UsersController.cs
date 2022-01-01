//
//  UsersController.cs
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
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData.Deltas;
using Microsoft.AspNetCore.OData.Formatter;
using Microsoft.AspNetCore.OData.Query;
using Microsoft.AspNetCore.OData.Results;
using Microsoft.AspNetCore.OData.Routing.Controllers;
using Microsoft.EntityFrameworkCore;

namespace Brizbee.Api.Controllers
{
    public class UsersController : ODataController
    {
        private readonly IConfiguration _configuration;
        private readonly SqlContext _context;

        public UsersController(IConfiguration configuration, SqlContext context)
        {
            _configuration = configuration;
            _context = context;
        }

        // GET: odata/Users
        [EnableQuery(PageSize = 100, MaxExpansionDepth = 1)]
        public IQueryable<User> GetUsers()
        {
            var currentUser = CurrentUser();

            // Ensure that user is authorized.
            if (!currentUser.CanViewUsers)
                return Enumerable.Empty<User>().AsQueryable();

            return _context.Users
                .Where(u => !u.IsDeleted)
                .Where(u => u.OrganizationId == currentUser.OrganizationId);
        }

        // GET: odata/Users(5)
        [EnableQuery]
        public SingleResult<User> GetUser([FromODataUri] int key)
        {
            var currentUser = CurrentUser();

            // Ensure that user is authorized.
            if (!currentUser.CanViewUsers && currentUser.Id != key)
                return SingleResult.Create(Enumerable.Empty<User>().AsQueryable());

            return SingleResult.Create(_context.Users
                .Where(u => !u.IsDeleted)
                .Where(u => u.OrganizationId == currentUser.OrganizationId)
                .Where(u => u.Id == key));
        }

        // POST: odata/Users
        public IActionResult Post([FromBody] User user)
        {
            var currentUser = CurrentUser();

            // Ensure that user is authorized.
            if (!currentUser.CanCreateUsers)
                return Forbid();

            // Formatting
            if (!string.IsNullOrEmpty(user.EmailAddress))
                user.EmailAddress = user.EmailAddress.ToLower();

            // Auto-generated
            user.CreatedAt = DateTime.UtcNow;
            user.OrganizationId = currentUser.OrganizationId;
            user.AllowedPhoneNumbers = "*";
            user.Role = "Standard";

            // Ensure that Pin is unique in the organization
            if (_context.Users
                .Where(u => u.OrganizationId == user.OrganizationId)
                .Where(u => u.Pin == user.Pin)
                .Where(u => u.IsDeleted == false)
                .Any())
            {
                throw new Exception("Another user in the organization already has that Pin");
            }

            if (!string.IsNullOrEmpty(user.Password))
            {
                // Generates a password hash and salt
                var service = new SecurityService();
                user.PasswordSalt = service.GenerateHash(service.GenerateRandomString());
                user.PasswordHash = service.GenerateHash(string.Format("{0} {1}", user.Password, user.PasswordSalt));
                user.Password = null;
            }
            else
            {
                user.Password = null;
            }

            // Validate the model.
            ModelState.ClearValidationState(nameof(user));
            if (!TryValidateModel(user, nameof(user)))
            {
                var errors = new List<string>();

                foreach (var modelStateKey in ModelState.Keys)
                {
                    var value = ModelState[modelStateKey];

                    if (value == null) continue;

                    foreach (var error in value.Errors)
                    {
                        errors.Add(error.ErrorMessage);
                    }
                }

                var message = string.Join(", ", errors);
                return BadRequest(message);
            }

            _context.Users.Add(user);

            _context.SaveChanges();

            return Ok(user);
        }

        // PATCH: odata/Users(5)
        public IActionResult Patch([FromODataUri] int key, Delta<User> patch)
        {
            var currentUser = CurrentUser();

            var user = _context.Users
                .Where(u => u.OrganizationId == currentUser.OrganizationId)
                .Where(u => !u.IsDeleted)
                .Where(u => u.Id == key)
                .FirstOrDefault();

            // Ensure that object was found.
            if (user == null) return NotFound();

            // Ensure that user is authorized.
            if (!currentUser.CanModifyUsers)
                return Forbid();

            // Do not allow modifying some properties
            if (patch.GetChangedPropertyNames().Contains("OrganizationId") ||
                patch.GetChangedPropertyNames().Contains("Id") ||
                patch.GetChangedPropertyNames().Contains("EmailAddress"))
            {
                throw new Exception("Cannot modify Id, OrganizationId, or EmailAddress");
            }

            // Ensure that Pin is unique in the organization
            if (patch.GetChangedPropertyNames().Contains("Pin"))
            {
                patch.TryGetPropertyValue("Pin", out object pin);
                var castedPin = pin as string;
                if (_context.Users
                    .Where(u => u.OrganizationId == user.OrganizationId)
                    .Where(u => u.Pin == castedPin)
                    .Where(u => u.IsDeleted == false)
                    .Where(u => u.Id != user.Id) // Do not include current user in determination
                    .Any())
                {
                    throw new Exception("Another user in the organization already has that Pin");
                }
            }

            // Peform the update
            patch.Patch(user);

            if (user.Password != null)
            {
                // Generates a password hash and salt
                var service = new SecurityService();
                user.PasswordSalt = service.GenerateHash(service.GenerateRandomString());
                user.PasswordHash = service.GenerateHash(string.Format("{0} {1}", user.Password, user.PasswordSalt));
                user.Password = null;
            }
            else
            {
                user.Password = null;
            }

            // Validate the model.
            ModelState.ClearValidationState(nameof(user));
            if (!TryValidateModel(user, nameof(user)))
                return BadRequest();

            _context.SaveChanges();

            return NoContent();
        }

        // DELETE: odata/Users(5)
        public IActionResult Delete([FromODataUri] int key)
        {
            var currentUser = CurrentUser();

            // Ensure that user is authorized.
            if (!currentUser.CanDeleteUsers)
                return Forbid();

            var user = _context.Users
                .Where(u => !u.IsDeleted)
                .Where(u => u.OrganizationId == currentUser.OrganizationId)
                .Where(u => u.Id == key)
                .FirstOrDefault();

            // Ensure that object was found.
            if (user == null) return NotFound();

            user.IsDeleted = true;

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