using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Security.Principal;
using System.Web;
using System.Web.Http;
using System.Web.Http.Controllers;

namespace Brizbee.Filters
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

            // for AllowAnonymous
            if (SkipAuthorization(actionContext))
            {
                return;
            }

            // verify the passed hash with the calculated one
            if (!AuthorizeRequest(actionContext))
            {
                // raises an unauthorized status code
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
                    Trace.TraceInformation("Using query parameter authentication");

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
                    Trace.TraceInformation("Using header authentication");

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

        private static bool SkipAuthorization(HttpActionContext actionContext)
        {
            if (!Enumerable.Any<AllowAnonymousAttribute>((IEnumerable<AllowAnonymousAttribute>)actionContext.ActionDescriptor.GetCustomAttributes<AllowAnonymousAttribute>()))
                return Enumerable.Any<AllowAnonymousAttribute>((IEnumerable<AllowAnonymousAttribute>)actionContext.ControllerContext.ControllerDescriptor.GetCustomAttributes<AllowAnonymousAttribute>());
            else
                return true;
        }
    }
}