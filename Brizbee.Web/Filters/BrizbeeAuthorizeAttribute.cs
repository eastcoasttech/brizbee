//
//  BrizbeeAuthorizeAttribute.cs
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

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Security.Principal;
using System.Web;
using System.Web.Http;
using System.Web.Http.Controllers;

namespace Brizbee.Web.Filters
{
    public class BrizbeeAuthorizeAttribute : AuthorizeAttribute
    {
        public override void OnAuthorization(HttpActionContext actionContext)
        {
            if (actionContext.Request.RequestUri.AbsolutePath == "/$metadata" ||
               actionContext.Request.RequestUri.AbsolutePath == "/%24metadata" ||
               actionContext.Request.RequestUri.AbsolutePath == "/odata/Users/Default.Authenticate" ||
               actionContext.Request.RequestUri.AbsolutePath == "/odata/Users/Default.Register")
            {
                return;
            }

            // Enable AllowAnonymous
            if (SkipAuthorization(actionContext))
            {
                return;
            }

            // Now, verify the passed hash with the calculated one
            if (!AuthorizeRequest(actionContext))
            {
                // Raises an unauthorized status code
                HandleUnauthorizedRequest(actionContext);
            }
        }

        protected override void HandleUnauthorizedRequest(HttpActionContext actionContext)
        {
            base.HandleUnauthorizedRequest(actionContext);
        }

        private bool AuthorizeRequest(HttpActionContext actionContext)
        {
            try
            {
                //uri is still accessible so use this to get query params
                var queryString = HttpUtility.ParseQueryString(actionContext.Request.RequestUri.Query);

                if (queryString["AuthUserId"] != null)
                {
                    // Query Authentication
                    var authUserId = queryString["AuthUserId"];
                    var authExpiration = queryString["AuthExpiration"];
                    var authToken = queryString["AuthToken"];

                    // Verify the hash in the headers and the calculated hash
                    var token = string.Format("{0} {1} {2}", "SECRET KEY", authUserId, authExpiration);
                    var calculatedToken = new SecurityService().GenerateHash(token);

                    if (authToken.Equals(calculatedToken))
                    {
                        var roles = new string[] { };
                        actionContext.RequestContext.Principal = new GenericPrincipal(new GenericIdentity(authUserId), roles);
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                }
                else
                {
                    // Header Authentication
                    IEnumerable<string> userIdHeaders = actionContext.Request.Headers.GetValues("AUTH_USER_ID");
                    var authUserId = userIdHeaders.FirstOrDefault();

                    IEnumerable<string> expirationHeaders = actionContext.Request.Headers.GetValues("AUTH_EXPIRATION");
                    var authExpiration = expirationHeaders.FirstOrDefault();

                    IEnumerable<string> tokenHeaders = actionContext.Request.Headers.GetValues("AUTH_TOKEN");
                    var authToken = tokenHeaders.FirstOrDefault();

                    // Verify the hash in the headers and the calculated hash
                    var token = string.Format("{0} {1} {2}", "SECRET KEY", authUserId, authExpiration);
                    var calculatedToken = new SecurityService().GenerateHash(token);

                    if (authToken.Equals(calculatedToken))
                    {
                        var roles = new string[] { };
                        actionContext.RequestContext.Principal = new GenericPrincipal(new GenericIdentity(authUserId), roles);
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                }
            }
            catch (Exception)
            {
                return false;
            }
        }

        /// <summary>
        /// Whether or not to skip authentication
        /// </summary>
        /// <param name="actionContext"></param>
        /// <returns></returns>
        private static bool SkipAuthorization(HttpActionContext actionContext)
        {
            if (!Enumerable.Any(actionContext.ActionDescriptor.GetCustomAttributes<AllowAnonymousAttribute>()))
                return Enumerable.Any(actionContext.ControllerContext.ControllerDescriptor.GetCustomAttributes<AllowAnonymousAttribute>());
            else
                return true;
        }
    }
}