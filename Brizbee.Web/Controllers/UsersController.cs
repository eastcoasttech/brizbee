using Brizbee.Common.Models;
using Brizbee.Common.Security;
using Brizbee.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web;
using System.Web.Http;
using System.Web.OData;

namespace Brizbee.Controllers
{
    public class UsersController : BaseODataController
    {
        private BrizbeeWebContext db = new BrizbeeWebContext();
        private UserRepository repo = new UserRepository();

        // POST: odata/Users/Default.Authenticate
        [HttpPost]
        public IHttpActionResult Authenticate(ODataActionParameters parameters)
        {
            var session = parameters["Session"] as Session;

            // Return a token, expiration, and user id. The
            // token and expiration are concatenated and verified
            // later as part of each request.
            var credential = new Credential();

            var service = new SecurityService();

            // Validate both an Email and Password
            if (session.EmailAddress == null || session.EmailPassword == null)
            {
                return BadRequest("Must provide both an Email Address and password");
            }

            var user = db.Users.Where(u => u.EmailAddress ==
                session.EmailAddress).FirstOrDefault();

            // Attempt to authenticate
            if ((user == null) ||
                !service.AuthenticateWithPassword(user,
                    session.EmailPassword))
            {
                return BadRequest("Invalid Email Address and password combination");
            }

            var now = DateTime.Now.Ticks.ToString();
            var token = string.Format("{0} {1} {2}", "SECRET KEY", user.Id.ToString(), now);
            credential.AuthToken = service.GenerateHash(token);
            credential.AuthExpiration = now;
            credential.AuthUserId = user.Id.ToString();
            
            return Ok(credential);
        }

        // POST: odata/Users(5)/ChangePassword
        [HttpPost]
        public IHttpActionResult ChangePassword([FromODataUri] int key, ODataActionParameters parameters)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest();
            }

            string password = (string)parameters["Password"];
            repo.ChangePassword(key, password, CurrentUser());
            return Ok();
        }

        // POST: odata/Users/Default.Register
        [AllowAnonymous]
        [HttpPost]
        public IHttpActionResult Register(ODataActionParameters parameters)
        {
            var organization = parameters["Organization"] as Organization;
            var user = parameters["User"] as User;

            var created = repo.Register(user, organization);

            return Ok(created);
        }
    }
}