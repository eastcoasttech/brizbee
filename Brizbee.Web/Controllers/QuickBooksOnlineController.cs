using Brizbee.Common.Models;
using Intuit.Ipp.Core;
using Intuit.Ipp.Data;
using Intuit.Ipp.DataService;
using Intuit.Ipp.OAuth2PlatformClient;
using Intuit.Ipp.QueryFilter;
using Intuit.Ipp.Security;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
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
            var url = string.Format("https://app.brizbee.com/#!/export-quickbooks-online?stateMessage={0}&errorMessage={1}&realmId={2}&accessToken={3}&accessTokenExpiresAt={4}&refreshToken={5}&refreshTokenExpiresAt={6}&step={7}",
                stateMessage, errorMessage, realmId, accessToken, accessTokenExpiresAt, refreshToken, refreshTokenExpiresAt, "company");
            response.Headers.Location = new Uri(url);
            return response;
        }

        // GET: api/QuickBooksOnline/CompanyInformation
        [HttpGet]
        [Route("api/QuickBooksOnline/CompanyInformation")]
        public HttpResponseMessage GetCompanyInformation(string realmId = "", string accessToken = "")
        {
            OAuth2RequestValidator oauthValidator = new OAuth2RequestValidator(accessToken);

            // Create a ServiceContext with Auth tokens and realmId
            ServiceContext serviceContext = new ServiceContext(realmId, IntuitServicesType.QBO, oauthValidator);
            serviceContext.IppConfiguration.MinorVersion.Qbo = "23";
            serviceContext.IppConfiguration.BaseUrl.Qbo = "https://sandbox-quickbooks.api.intuit.com/";

            // Create a QuickBooks QueryService using ServiceContext
            QueryService<CompanyInfo> querySvc = new QueryService<CompanyInfo>(serviceContext);
            CompanyInfo companyInfo = querySvc.ExecuteIdsQuery("SELECT * FROM CompanyInfo").FirstOrDefault();

            var response = Request.CreateResponse(HttpStatusCode.OK);
            response.Content = new StringContent(companyInfo.CompanyName, System.Text.Encoding.UTF8, "application/xml");
            return response;
        }

        // POST: api/QuickBooksOnline/TimeActivities
        [HttpPost]
        [Route("api/QuickBooksOnline/TimeActivities")]
        public HttpResponseMessage PostTimeActivities(string realmId = "", string accessToken = "", int? commitId = null, DateTime? inAt = null, DateTime? outAt = null)
        {
            try
            {
                var currentUser = CurrentUser();
                var export = new QuickBooksOnlineExport()
                {
                    CreatedAt = DateTime.UtcNow,
                    UserId = currentUser.Id
                };

                OAuth2RequestValidator oauthValidator = new OAuth2RequestValidator(accessToken);

                // Create a ServiceContext with Auth tokens and realmId
                ServiceContext serviceContext = new ServiceContext(realmId, IntuitServicesType.QBO, oauthValidator);
                serviceContext.IppConfiguration.MinorVersion.Qbo = "23";
                serviceContext.IppConfiguration.BaseUrl.Qbo = "https://sandbox-quickbooks.api.intuit.com/";

                // DataService is used to create objects
                DataService dataService = new DataService(serviceContext);

                // Create a QuickBooks QueryService using ServiceContext
                QueryService<Employee> employeeSvc = new QueryService<Employee>(serviceContext);

                DateTime parsedInAt;
                DateTime parsedOutAt;
                if (commitId.HasValue)
                {
                    var commit = db.Commits.Find(commitId.Value);
                    parsedInAt = commit.InAt;
                    parsedOutAt = commit.OutAt;
                    export.InAt = parsedInAt;
                    export.OutAt = parsedOutAt;
                    export.CommitId = commitId.Value;
                }
                else if (inAt.HasValue && outAt.HasValue)
                {
                    parsedInAt = inAt.Value;
                    parsedOutAt = outAt.Value;
                    export.InAt = parsedInAt;
                    export.OutAt = parsedOutAt;
                }
                else
                {
                    throw new Exception("Must specify a commit id or a range to export time activities.");
                }

                var punches = db.Punches
                    .Include("User")
                    .Where(p => p.InAt >= parsedInAt && p.OutAt.HasValue && p.OutAt <= parsedOutAt);
                foreach (var punch in punches)
                {
                    var displayName = punch.User.QuickBooksEmployee.Replace("'", "\\'"); //Escape special characters
                    var query = string.Format("SELECT * FROM Employee WHERE DisplayName = '{0}'", displayName);
                    Employee employee = employeeSvc.ExecuteIdsQuery(query).FirstOrDefault();

                    //if (employee != null)
                    //{
                    //}

                    var time = new TimeActivity()
                    {
                        TxnDate = punch.InAt,
                        TxnDateSpecified = true,
                        StartTime = punch.InAt,
                        EndTime = punch.OutAt.Value,
                        NameOf = TimeActivityTypeEnum.Employee,
                        NameOfSpecified = true,
                        //ItemElementName = ItemChoiceType5.EmployeeRef,
                        //ItemRef
                        AnyIntuitObject = new ReferenceType()
                        {
                            name = employee.DisplayName,
                            Value = employee.Id
                        },
                        HoursSpecified = false,
                        EndTimeSpecified = true,
                        StartTimeSpecified = true
                    };
                    dataService.Add(time);
                }

                var response = Request.CreateResponse(HttpStatusCode.Created);
                response.Content = new StringContent("", System.Text.Encoding.UTF8, "application/xml");
                return response;
            }
            catch (Exception ex)
            {
                var response = Request.CreateResponse(HttpStatusCode.InternalServerError);
                response.Content = new StringContent(ex.ToString(), System.Text.Encoding.UTF8, "application/xml");
                return response;
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

        public Common.Models.User CurrentUser()
        {
            if (ActionContext.RequestContext.Principal.Identity.Name.Length > 0)
            {
                var currentUserId = int.Parse(ActionContext.RequestContext.Principal.Identity.Name);
                return db.Users
                    .Where(u => u.Id == currentUserId)
                    .FirstOrDefault();
            }
            else
            {
                return null;
            }
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