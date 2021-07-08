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
using Brizbee.Web.Repositories;
using Microsoft.AspNet.OData;
using System;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Web.Http;

namespace Brizbee.Web.Controllers
{
    public class UsersController : BaseODataController
    {
        private SqlContext db = new SqlContext();
        private UserRepository repo = new UserRepository();

        // GET: odata/Users
        [EnableQuery(PageSize = 100, MaxExpansionDepth = 1)]
        public IQueryable<User> GetUsers()
        {
            var currentUser = CurrentUser();

            // Only permit administrators to see all users
            if (currentUser.Role != "Administrator")
            {
                return Enumerable.Empty<User>().AsQueryable();
            }

            return db.Users
                .Where(u => !u.IsDeleted)
                .Where(u => u.OrganizationId == currentUser.OrganizationId);
        }

        // GET: odata/Users(5)
        [EnableQuery]
        public SingleResult<User> GetUser([FromODataUri] int key)
        {
            var currentUser = CurrentUser();

            // Only Administrators can see other users in the organization
            if (currentUser.Role != "Administrator" && currentUser.Id != key)
            {
                return SingleResult.Create(Enumerable.Empty<User>().AsQueryable());
            }

            return SingleResult.Create(db.Users
                .Where(u => !u.IsDeleted)
                .Where(u => u.OrganizationId == currentUser.OrganizationId)
                .Where(u => u.Id == key));
        }

        // POST: odata/Users
        public IHttpActionResult Post(User user)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var currentUser = CurrentUser();

            // Ensure user is authorized
            if (currentUser.Role != "Administrator")
                return BadRequest();

            // Formatting
            if (!string.IsNullOrEmpty(user.EmailAddress))
                user.EmailAddress = user.EmailAddress.ToLower();

            // Auto-generated
            user.CreatedAt = DateTime.UtcNow;
            user.OrganizationId = currentUser.OrganizationId;
            user.AllowedPhoneNumbers = "*";

            // Ensure that Pin is unique in the organization
            if (db.Users
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

            db.Users.Add(user);

            db.SaveChanges();

            return Created(user);
        }

        // PATCH: odata/Users(5)
        [AcceptVerbs("PATCH", "MERGE")]
        public IHttpActionResult Patch([FromODataUri] int key, Delta<User> patch)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var currentUser = CurrentUser();

            var user = db.Users
                .Where(u => !u.IsDeleted)
                .Where(u => u.Id == key)
                .FirstOrDefault();

            // Do not allow modifying some properties
            if (patch.GetChangedPropertyNames().Contains("OrganizationId") ||
                patch.GetChangedPropertyNames().Contains("EmailAddress"))
            {
                throw new Exception("Cannot modify OrganizationId or EmailAddress");
            }

            // Ensure that Pin is unique in the organization
            if (patch.GetChangedPropertyNames().Contains("Pin"))
            {
                patch.TryGetPropertyValue("Pin", out object pin);
                var castedPin = pin as string;
                if (db.Users
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

            db.SaveChanges();

            return Updated(user);
        }

        // DELETE: odata/Users(5)
        public IHttpActionResult Delete([FromODataUri] int key)
        {
            var currentUser = CurrentUser();

            // Ensure user is authorized
            if (currentUser.Role != "Administrator")
                return BadRequest();

            var user = db.Users
                .Where(u => !u.IsDeleted)
                .Where(u => u.OrganizationId == currentUser.OrganizationId)
                .Where(u => u.Id == key)
                .FirstOrDefault();

            if (user == null)
            {
                return StatusCode(HttpStatusCode.NotFound);
            }

            user.IsDeleted = true;

            db.SaveChanges();

            return StatusCode(HttpStatusCode.NoContent);
        }

        // POST: odata/Users/Default.Authenticate
        [HttpPost]
        public IHttpActionResult Authenticate(ODataActionParameters parameters)
        {
            Session session = parameters["Session"] as Session;
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

                    user = db.Users
                        .Where(u => u.EmailAddress == session.EmailAddress)
                        .Where(u => u.IsDeleted == false)
                        .FirstOrDefault();

                    // Attempt to authenticate
                    if ((user == null) ||
                        !service.AuthenticateWithPassword(user,
                            session.EmailPassword))
                    {
                        return BadRequest("Invalid Email Address and password combination");
                    }

                    return Content(HttpStatusCode.Created, GetCredentials(user));
                    //return Ok(GetCredentials(user));
                case "pin":
                    // Validate both an organization code and user pin
                    if (session.PinUserPin == null || session.PinOrganizationCode == null)
                    {
                        return BadRequest("Must provide both an organization code and user PIN");
                    }
                    
                    user = db.Users.Include("Organization")
                        .Where(u => u.Pin == session.PinUserPin.ToUpper())
                        .Where(u => u.Organization.Code == session.PinOrganizationCode.ToUpper())
                        .Where(u => u.IsDeleted == false)
                        .FirstOrDefault();

                    // Attempt to authenticate
                    if (user == null)
                    {
                        return BadRequest("Invalid organization code and user pin combination");
                    }

                    return Content(HttpStatusCode.Created, GetCredentials(user));
                    //return Ok(GetCredentials(user));
                default:
                    return BadRequest("Must authenticate via either Email or Pin method");
            }
        }

        // POST: odata/Users(5)/ChangePassword
        [HttpPost]
        public IHttpActionResult ChangePassword([FromODataUri] int key, ODataActionParameters parameters)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest();
            }

            var currentUser = CurrentUser();

            string password = (string)parameters["Password"];

            var user = db.Users
                .Where(u => !u.IsDeleted)
                .Where(u => u.Id == key)
                .FirstOrDefault();

            // Ensure user is authorized
            if (currentUser.Role != "Administrator" || currentUser.Id != user.Id)
                return BadRequest();

            // Generates a password hash and salt
            var service = new SecurityService();
            user.PasswordSalt = service.GenerateHash(service.GenerateRandomString());
            user.PasswordHash = service.GenerateHash(string.Format("{0} {1}", password, user.PasswordSalt));
            user.Password = null;

            db.SaveChanges();

            return Ok();
        }

        // POST: odata/Users/Default.Register
        [HttpPost]
        public IHttpActionResult Register(ODataActionParameters parameters)
        {
            var organization = parameters["Organization"] as Organization;
            var user = parameters["User"] as User;

            var registered = repo.Register(user, organization);

            return Created(registered);
        }

        private Credential GetCredentials(User user)
        {
            // Return a token, expiration, and user id. The
            // token and expiration are concatenated and verified
            // later as part of each request.
            var credential = new Credential();

            var service = new SecurityService();

            var now = DateTime.UtcNow.Ticks.ToString();
            var token = string.Format("{0} {1} {2}", "SECRET KEY", user.Id.ToString(), now);
            credential.AuthToken = service.GenerateHash(token);
            credential.AuthExpiration = now;
            credential.AuthUserId = user.Id.ToString();

            return credential;
        }

        /// <summary>
        /// Disposes of the resources used during each request (instance)
        /// of this controller.
        /// </summary>
        /// <param name="disposing">Whether or not the resources should be disposed</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
                repo.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}