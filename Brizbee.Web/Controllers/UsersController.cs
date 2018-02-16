using Brizbee.Common.Models;
using Brizbee.Common.Security;
using Brizbee.Repositories;
using System;
using System.Collections.Generic;
using System.Diagnostics;
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

        // GET: odata/Users
        [EnableQuery(PageSize = 20, MaxExpansionDepth = 1)]
        public IQueryable<User> GetUsers()
        {
            try
            {
                return repo.GetAll(CurrentUser());
            }
            catch (Exception)
            {
                // Return an empty result if there are errors
                return Enumerable.Empty<User>().AsQueryable();
            }
        }

        // GET: odata/Users(5)
        [EnableQuery]
        public SingleResult<User> GetUser([FromODataUri] int key)
        {
            try
            {
                var queryable = new List<User>() { repo.Get(key, CurrentUser()) }.AsQueryable();
                return SingleResult.Create(queryable);
            }
            catch (Exception)
            {
                // Return an empty result if there are errors
                return SingleResult.Create(Enumerable.Empty<User>().AsQueryable());
            }
        }

        // POST: odata/Users
        public IHttpActionResult Post(User user)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            user = repo.Create(user, CurrentUser());

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

            var user = repo.Update(key, patch, CurrentUser());

            return Updated(user);
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

                    user = db.Users.Where(u => u.EmailAddress ==
                        session.EmailAddress).FirstOrDefault();

                    // Attempt to authenticate
                    if ((user == null) ||
                        !service.AuthenticateWithPassword(user,
                            session.EmailPassword))
                    {
                        return BadRequest("Invalid Email Address and password combination");
                    }
                    
                    return Ok(GetCredentials(user));
                case "pin":
                    // Validate both an organization code and user pin
                    if (session.PinUserPin == null || session.PinOrganizationCode == null)
                    {
                        return BadRequest("Must provide both an organization code and user PIN");
                    }
                    
                    user = db.Users.Include("Organization")
                        .Where(u => u.Pin == session.PinUserPin.ToUpper())
                        .Where(u => u.Organization.Code ==
                            session.PinOrganizationCode.ToUpper())
                        .FirstOrDefault();

                    // Attempt to authenticate
                    if (user == null)
                    {
                        return BadRequest("Invalid organization code and user pin combination");
                    }

                    return Ok(GetCredentials(user));
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

        private Credential GetCredentials(User user)
        {
            // Return a token, expiration, and user id. The
            // token and expiration are concatenated and verified
            // later as part of each request.
            var credential = new Credential();

            var service = new SecurityService();

            var now = DateTime.Now.Ticks.ToString();
            var token = string.Format("{0} {1} {2}", "SECRET KEY", user.Id.ToString(), now);
            credential.AuthToken = service.GenerateHash(token);
            credential.AuthExpiration = now;
            credential.AuthUserId = user.Id.ToString();

            return credential;
        }
    }
}