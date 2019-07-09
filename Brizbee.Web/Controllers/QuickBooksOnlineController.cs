using Intuit.Ipp.Core;
using Intuit.Ipp.Data;
using Intuit.Ipp.OAuth2PlatformClient;
using Intuit.Ipp.QueryFilter;
using Intuit.Ipp.Security;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Web;
using System.Web.Http;

namespace Brizbee.Web.Controllers
{
    public class QuickBooksOnlineController : ApiController
    {
        public static string clientid = ConfigurationManager.AppSettings["clientid"];
        public static string clientsecret = ConfigurationManager.AppSettings["clientsecret"];
        public static string redirectUrl = ConfigurationManager.AppSettings["redirectUrl"];
        public static string environment = ConfigurationManager.AppSettings["appEnvironment"];

        public OAuth2Client auth2Client = new OAuth2Client(clientid, clientsecret, redirectUrl, environment);

        private BrizbeeWebContext db = new BrizbeeWebContext();

        // POST: api/QuickBooksOnline/Authenticate
        [HttpPost]
        [AllowAnonymous]
        [Route("api/QuickBooksOnline/Authenticate")]
        public HttpResponseMessage PostAuthenticate()
        {
            List<OidcScopes> scopes = new List<OidcScopes>();
            scopes.Add(OidcScopes.Accounting);
            string authorizeUrl = auth2Client.GetAuthorizationURL(scopes);

            var response = Request.CreateResponse(HttpStatusCode.Moved);
            response.Headers.Location = new Uri(authorizeUrl);
            return response;
        }

        // GET: api/QuickBooksOnline/Callback
        [HttpGet]
        [AllowAnonymous]
        [Route("api/QuickBooksOnline/Callback")]
        public async Task<HttpResponseMessage> GetCallback(string code = "none", string error = "none", string realmId = "none", string state = "")
        {
            var stateMessage = "";
            var errorMessage = "";
            var accessToken = "";
            var accessTokenExpiresAt = "";
            var refreshToken = "";
            var refreshTokenExpiresAt = "";

            // Sync the state info and update if it is not the same
            if (state.Equals(QuickAuthController.auth2Client.CSRFToken, StringComparison.Ordinal))
            {
                stateMessage = state + " (valid)";
            }
            else
            {
                stateMessage = state + " (invalid)";
            }

            //await GetAuthTokensAsync(code, realmId);
            var tokenResponse = await QuickAuthController.auth2Client.GetBearerTokenAsync(code);

            if (!string.IsNullOrWhiteSpace(tokenResponse.AccessToken))
            {
                accessToken = tokenResponse.AccessToken;
                accessTokenExpiresAt = (DateTime.Now.AddSeconds(tokenResponse.AccessTokenExpiresIn)).ToString();
            }

            if (!string.IsNullOrWhiteSpace(tokenResponse.RefreshToken))
            {
                refreshToken = tokenResponse.RefreshToken;
                refreshTokenExpiresAt = (DateTime.Now.AddSeconds(tokenResponse.RefreshTokenExpiresIn)).ToString();
            }

            errorMessage = error;

            var response = Request.CreateResponse(HttpStatusCode.Found); // or Moved
            var url = string.Format("https://app.brizbee.com/#!/export?stateMessage={0}&errorMessage={1}&realmId={2}&accessToken={3}&accessTokenExpiresAt={4}&refreshToken={5}&refreshTokenExpiresAt={6}",
                stateMessage, errorMessage, realmId, accessToken, accessTokenExpiresAt, refreshToken, refreshTokenExpiresAt);
            response.Headers.Location = new Uri(url);
            return response;
        }

        // GET: api/QuickBooksOnline/CompanyInformation
        [HttpGet]
        [AllowAnonymous]
        [Route("api/QuickBooksOnline/CompanyInformation")]
        public HttpResponseMessage GetCompanyInformation(string realmId = "", string accessToken = "")
        {
            try
            {
                //var principal = User as ClaimsPrincipal;
                //OAuth2RequestValidator oauthValidator = new OAuth2RequestValidator(principal.FindFirst("access_token").Value);
                OAuth2RequestValidator oauthValidator = new OAuth2RequestValidator(accessToken);

                // Create a ServiceContext with Auth tokens and realmId
                ServiceContext serviceContext = new ServiceContext(realmId, IntuitServicesType.QBO, oauthValidator);
                serviceContext.IppConfiguration.MinorVersion.Qbo = "23";
                serviceContext.IppConfiguration.BaseUrl.Qbo = "https://sandbox-quickbooks.api.intuit.com/";

                // Create a QuickBooks QueryService using ServiceContext
                QueryService<CompanyInfo> querySvc = new QueryService<CompanyInfo>(serviceContext);
                CompanyInfo companyInfo = querySvc.ExecuteIdsQuery("SELECT * FROM CompanyInfo").FirstOrDefault();

                string output = "Company Name: " + companyInfo.CompanyName + " Company Address: " + companyInfo.CompanyAddr.Line1 + ", " + companyInfo.CompanyAddr.City + ", " + companyInfo.CompanyAddr.Country + " " + companyInfo.CompanyAddr.PostalCode;
                //return View("ApiCallService", (object)("QBO API call Successful!! Response: " + output));
                var response = Request.CreateResponse(HttpStatusCode.OK);
                response.Content = new StringContent(output, System.Text.Encoding.UTF8, "application/xml");
                return response;
            }
            catch (Exception ex)
            {
                var response = Request.CreateResponse(HttpStatusCode.InternalServerError);
                response.Content = new StringContent(ex.ToString(), System.Text.Encoding.UTF8, "application/xml");
                return response;
                //return View("ApiCallService", (object)("QBO API call Failed!" + " Error message: " + ex.Message));
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }

        /// <summary>
        /// Exchange Auth code with Auth Access and Refresh tokens and add them to Claim list
        /// </summary>
        private async System.Threading.Tasks.Task GetAuthTokensAsync(string code, string realmId)
        {
            //if (realmId != null)
            //{
            //    Session["realmId"] = realmId;
            //}

            //Request.GetOwinContext().Authentication.SignOut("TempState");
            var tokenResponse = await QuickAuthController.auth2Client.GetBearerTokenAsync(code);

            //var claims = new List<Claim>();

            //if (Session["realmId"] != null)
            //{
            //    claims.Add(new Claim("realmId", Session["realmId"].ToString()));
            //}

            if (!string.IsNullOrWhiteSpace(tokenResponse.AccessToken))
            {
                var accessToken = tokenResponse.AccessToken;
                var accessTokenExpiresAt = (DateTime.Now.AddSeconds(tokenResponse.AccessTokenExpiresIn)).ToString();
                //claims.Add(new Claim("access_token", tokenResponse.AccessToken));
                //claims.Add(new Claim("access_token_expires_at", (DateTime.Now.AddSeconds(tokenResponse.AccessTokenExpiresIn)).ToString()));
            }

            if (!string.IsNullOrWhiteSpace(tokenResponse.RefreshToken))
            {
                var refreshToken = tokenResponse.RefreshToken;
                var refreshTokenExpiresAt = (DateTime.Now.AddSeconds(tokenResponse.RefreshTokenExpiresIn)).ToString();
                //claims.Add(new Claim("refresh_token", tokenResponse.RefreshToken));
                //claims.Add(new Claim("refresh_token_expires_at", (DateTime.Now.AddSeconds(tokenResponse.RefreshTokenExpiresIn)).ToString()));
            }

            //var id = new ClaimsIdentity(claims, "Cookies");
            //Request.GetOwinContext().Authentication.SignIn(id);
        }
    }
}