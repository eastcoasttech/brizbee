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
using System.Configuration;
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

            // Ensure that user is authorized.
            if (!currentUser.CanViewUsers)
                return Enumerable.Empty<User>().AsQueryable();

            return db.Users
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

            // Ensure that user is authorized.
            if (!currentUser.CanCreateUsers)
                return StatusCode(HttpStatusCode.Forbidden);

            // Formatting
            if (!string.IsNullOrEmpty(user.EmailAddress))
                user.EmailAddress = user.EmailAddress.ToLower();

            // Auto-generated
            user.CreatedAt = DateTime.UtcNow;
            user.OrganizationId = currentUser.OrganizationId;
            user.AllowedPhoneNumbers = "*";
            user.Role = "Standard";

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

            // Ensure that user is authorized.
            if (!currentUser.CanModifyUsers)
                return StatusCode(HttpStatusCode.Forbidden);

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

            // Ensure that user is authorized.
            if (!currentUser.CanDeleteUsers)
                return StatusCode(HttpStatusCode.Forbidden);

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

            if ("EMAIL" == session.Method.ToUpperInvariant())
            {
                var service = new SecurityService();

                // Validate both an Email and Password.
                if (session.EmailAddress == null || session.EmailPassword == null)
                    return BadRequest("Must provide both an Email address and password.");

                var found = db.Users
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

                var found = db.Users
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

            // Generate an organization code if one is not provided.
            var code = organization.Code;
            while (string.IsNullOrEmpty(code))
            {
                var generated = GetRandomNumber();

                var found = db.Organizations
                    .Where(o => o.Code == generated);

                if (!found.Any())
                    code = generated;
            }
            organization.Code = code;

            // Generate a user pin if one is not provided.
            if (string.IsNullOrEmpty(user.Pin))
                user.Pin = GetRandomNumber();

            var registered = repo.Register(user, organization);

            return Created(registered);
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