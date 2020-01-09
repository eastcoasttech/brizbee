using Brizbee.Common.Models;
using Intuit.Ipp.Core;
using Intuit.Ipp.Data;
using Intuit.Ipp.DataService;
using Intuit.Ipp.OAuth2PlatformClient;
using Intuit.Ipp.QueryFilter;
using Intuit.Ipp.Security;
using Newtonsoft.Json.Linq;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Http;
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
        private string baseUrl = "https://sandbox-quickbooks.api.intuit.com";

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

        // POST: api/QuickBooksOnline/ExportCommit
        [HttpPost]
        [Route("api/QuickBooksOnline/ExportCommit")]
        public HttpResponseMessage PostExportCommit(int commitId, string realmId = "", string accessToken = "")
        {
            try
            {
                var currentUser = CurrentUser();
                var export = new QuickBooksOnlineExport()
                {
                    CreatedAt = DateTime.UtcNow,
                    UserId = currentUser.Id
                };

                var commit = db.Commits.Find(commitId);
                DateTime parsedInAt = commit.InAt;
                DateTime parsedOutAt = commit.OutAt;
                export.InAt = parsedInAt;
                export.OutAt = parsedOutAt;
                export.CommitId = commit.Id;

                var punches = db.Punches
                    .Include("User")
                    .Include("Task")
                    .Include("Task.Job")
                    .Include("Task.Job.Customer")
                    .Where(p => p.CommitId == commitId)
                    .Where(p => p.InAt >= parsedInAt && p.OutAt.HasValue && p.OutAt <= parsedOutAt)
                    .ToList();

                // Setup HTTP client with base URL and authentication
                var client = new RestClient(baseUrl);
                client.AddDefaultHeader("Authorization", string.Format("Bearer {0}", accessToken));

                // ------------------------------------------------------------
                // Verify that the employees exist in QuickBooks Online
                // ------------------------------------------------------------
                var qboEmployees = VerifyEmployeesExist(punches, realmId, client);

                // ------------------------------------------------------------
                // Verify that the customers exist in QuickBooks Online
                // ------------------------------------------------------------
                var qboCustomers = VerifyCustomersExist(punches, realmId, client);

                // ------------------------------------------------------------
                // Verify that the service items exist in QuickBooks Online
                // ------------------------------------------------------------
                var qboServiceItems = VerifyServiceItemsExist(punches, realmId, client);

                // ------------------------------------------------------------
                // Export the punches to QuickBooks Online
                // ------------------------------------------------------------
                var createdIds = new List<string>();

                // Loop the punches in groups of 30 and send the request
                for (int i = 0; i < punches.Count; i = i + 30)
                {
                    var subset = punches.OrderBy(p => p.Id).Skip(i).Take(30).ToArray();
                    var batch = new List<object>();
                    foreach (var punch in subset)
                    {
                        var employee = qboEmployees
                            .Where(x => x.DisplayName == punch.User.QuickBooksEmployee)
                            .FirstOrDefault();
                        var customer = qboCustomers
                            .Where(x => x.DisplayName == punch.Task.Job.QuickBooksCustomerJob)
                            .FirstOrDefault();
                        //var payrollItem = qboPayrollItems
                        //    .Where(i => i.DisplayName == punch.Task.QuickBooksPayrollItem)
                        //    .FirstOrDefault();
                        var serviceItem = qboServiceItems
                            .Where(x => x.Name == punch.Task.QuickBooksServiceItem)
                            .FirstOrDefault();

                        batch.Add(new
                        {
                            bId = Guid.NewGuid().ToString(),
                            operation = "create",
                            TimeActivity = new
                            {
                                CustomerRef = new
                                {
                                    name = customer.DisplayName,
                                    value = customer.Id
                                },
                                EmployeeRef = new
                                {
                                    name = employee.DisplayName,
                                    value = employee.Id
                                },
                                ItemRef = new
                                {
                                    name = serviceItem.Name,
                                    value = serviceItem.Id
                                },
                                //PayrollItemRef = new
                                //{
                                //    name = "",
                                //    value = ""
                                //},
                                EndTime = punch.OutAt.Value.ToString("yyyy-MM-ddTHH:mm:ss"), // 2013-07-05T17:00:00-08:00
                                NameOf = "Employee",
                                StartTime = punch.InAt,
                                TxnDate = punch.InAt.ToString("yyyy-MM-dd")
                            }
                        });
                    }

                    // Build request to create time activities in batches of 30
                    var timeActivityRequestUrl = string.Format("/v3/company/{0}/batch", realmId);
                    var batchRequest = new RestRequest(timeActivityRequestUrl, Method.POST);
                    batchRequest.AddJsonBody(new
                    {
                        BatchItemRequest = batch.ToArray()
                    });

                    // Execute the request
                    var batchResponse = client.Execute(batchRequest);
                    if ((batchResponse.ResponseStatus == ResponseStatus.Completed) &&
                            (batchResponse.StatusCode == HttpStatusCode.OK))
                    {
                        // Do something with success
                        JObject parsed = JObject.Parse(batchResponse.Content);
                        var parsedBatchItemResponse = parsed["BatchItemResponse"];
                        if (parsedBatchItemResponse != null)
                        {
                            foreach (var batchItem in parsedBatchItemResponse.Children())
                            {
                                var timeActivity = batchItem["TimeActivity"];
                                var id = timeActivity["Id"].ToString();
                                createdIds.Add(id);
                            }
                        }
                    }
                    else
                    {
                        // Do something with the error
                        throw new Exception(string.Format("Could not batch create time activities in QuickBooks Online: {0}", batchResponse.Content));
                    }
                    
                    System.Threading.Thread.Sleep(500);
                }

                // Save the new time activity ids to the export details
                export.CreatedTimeActivitiesIds = string.Join(",", createdIds);
                db.QuickBooksOnlineExports.Add(export);
                db.SaveChanges();

                var response = Request.CreateResponse(HttpStatusCode.Created);
                response.Content = new StringContent("", System.Text.Encoding.UTF8, "application/json");
                return response;
            }
            catch (Exception)
            {
                throw;
            }

            //try
            //{
            //    var currentUser = CurrentUser();
            //    var export = new QuickBooksOnlineExport()
            //    {
            //        CreatedAt = DateTime.UtcNow,
            //        UserId = currentUser.Id
            //    };

            //    OAuth2RequestValidator oauthValidator = new OAuth2RequestValidator(accessToken);

            //    // Create a ServiceContext with Auth tokens and realmId
            //    ServiceContext serviceContext = new ServiceContext(realmId, IntuitServicesType.QBO, oauthValidator);
            //    serviceContext.IppConfiguration.MinorVersion.Qbo = "23";
            //    serviceContext.IppConfiguration.BaseUrl.Qbo = "https://sandbox-quickbooks.api.intuit.com/";

            //    // DataService is used to create objects
            //    DataService dataService = new DataService(serviceContext);

            //    // Create a QuickBooks QueryService using ServiceContext
            //    QueryService<Employee> employeeSvc = new QueryService<Employee>(serviceContext);

            //    DateTime parsedInAt;
            //    DateTime parsedOutAt;
            //    if (commitId.HasValue)
            //    {
            //        var commit = db.Commits.Find(commitId.Value);
            //        parsedInAt = commit.InAt;
            //        parsedOutAt = commit.OutAt;
            //        export.InAt = parsedInAt;
            //        export.OutAt = parsedOutAt;
            //        export.CommitId = commitId.Value;
            //    }
            //    //else if (inAt.HasValue && outAt.HasValue)
            //    //{
            //    //    parsedInAt = inAt.Value;
            //    //    parsedOutAt = outAt.Value;
            //    //    export.InAt = parsedInAt;
            //    //    export.OutAt = parsedOutAt;
            //    //}
            //    else
            //    {
            //        throw new Exception("Must specify a commit id or a range to export time activities.");
            //    }

            //    var punches = db.Punches
            //        .Include("User")
            //        .Where(p => p.CommitId == commitId.Value)
            //        .Where(p => p.InAt >= parsedInAt && p.OutAt.HasValue && p.OutAt <= parsedOutAt);
            //    foreach (var punch in punches)
            //    {
            //        var displayName = punch.User.QuickBooksEmployee.Replace("'", "\\'"); //Escape special characters
            //        var query = string.Format("SELECT * FROM Employee WHERE DisplayName = '{0}'", displayName);
            //        Employee employee = employeeSvc.ExecuteIdsQuery(query).FirstOrDefault();


            //        if (employee != null)
            //        {
            //            var time = new TimeActivity()
            //            {
            //                TxnDate = punch.InAt,
            //                TxnDateSpecified = true,
            //                StartTime = punch.InAt,
            //                EndTime = punch.OutAt.Value,
            //                NameOf = TimeActivityTypeEnum.Employee,
            //                NameOfSpecified = true,
            //                //ItemElementName = ItemChoiceType5.EmployeeRef,
            //                //ItemRef
            //                //PayrollItemRef = 
            //                AnyIntuitObject = new ReferenceType()
            //                {
            //                    name = employee.DisplayName,
            //                    Value = employee.Id
            //                },
            //                HoursSpecified = false,
            //                EndTimeSpecified = true,
            //                StartTimeSpecified = true
            //            };
            //            dataService.Add(time);
            //        }
            //    }

            //    var response = Request.CreateResponse(HttpStatusCode.Created);
            //    response.Content = new StringContent("", System.Text.Encoding.UTF8, "application/xml");
            //    return response;
            //}
            //catch (Exception ex)
            //{
            //    var response = Request.CreateResponse(HttpStatusCode.InternalServerError);
            //    response.Content = new StringContent(ex.ToString(), System.Text.Encoding.UTF8, "application/xml");
            //    return response;
            //}
        }

        // POST: api/QuickBooksOnline/ReverseCommit
        [HttpPost]
        [Route("api/QuickBooksOnline/ReverseCommit")]
        public HttpResponseMessage PostReverseCommit(int commitId, string realmId = "", string accessToken = "")
        {
            var currentUser = CurrentUser();
            var export = db.QuickBooksOnlineExports.Where(e => e.CommitId == commitId).FirstOrDefault();

            // Setup HTTP client with base URL and authentication
            var client = new RestClient(baseUrl);
            client.AddDefaultHeader("Authorization", string.Format("Bearer {0}", accessToken));

            var createdIds = export.CreatedTimeActivitiesIds.Split(',');

            // Loop the time activities in groups of 30 and send the request
            for (int i = 0; i < createdIds.Length; i = i + 30)
            {
                var subset = createdIds.Skip(i).Take(30).ToArray();
                var batch = new List<object>();
                foreach (var id in subset)
                {
                    batch.Add(new
                    {
                        bId = Guid.NewGuid().ToString(),
                        operation = "delete",
                        TimeActivity = new
                        {
                            SyncToken = "0",
                            Id = id
                        }
                    });
                }

                // Build request to delete time activities in batches of 30
                var timeActivityRequestUrl = string.Format("/v3/company/{0}/batch", realmId);
                var batchRequest = new RestRequest(timeActivityRequestUrl, Method.POST);
                batchRequest.AddJsonBody(new
                {
                    BatchItemRequest = batch.ToArray()
                });

                // Execute the request
                var batchResponse = client.Execute(batchRequest);
                if ((batchResponse.ResponseStatus == ResponseStatus.Completed) &&
                        (batchResponse.StatusCode == HttpStatusCode.OK))
                {
                    // Do something with success
                }
                else
                {
                    // Do something with the error
                    throw new Exception(string.Format("Could not batch delete time activities in QuickBooks Online: {0}", batchResponse.Content));
                }

                System.Threading.Thread.Sleep(500);
            }

            var response = Request.CreateResponse(HttpStatusCode.Created);
            response.Content = new StringContent("", System.Text.Encoding.UTF8, "application/json");
            return response;
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

        private List<Serialization.QBO.Employee> VerifyEmployeesExist(List<Punch> punches, string realmId, RestClient client)
        {
            // Get all the employee names to query QBO
            var employeeIds = punches
                .GroupBy(p => p.UserId)
                .Select(g => g.Key)
                .ToArray();
            var brzEmployees = db.Users
                .Where(u => employeeIds.Contains(u.Id))
                .Select(u => new Serialization.QBO.Employee()
                {
                    DisplayName = u.QuickBooksEmployee,
                    Id = "" // Unknown at the moment
                })
                .ToList();
            var qboEmployees = new List<Serialization.QBO.Employee>();

            // Build request to query employees
            var criteria = string.Join(",", brzEmployees.Select(x => string.Format("'{0}'", x.DisplayName)).ToArray());
            var query = string.Format("SELECT * FROM Employee WHERE DisplayName IN ({0})", criteria);
            var requestUrl = string.Format("/v3/company/{0}/query", realmId);
            var request = new RestRequest(requestUrl, Method.GET);
            request.AddParameter("query", query, ParameterType.QueryString);
            request.AddHeader("Content-Type", "text/plain");

            // Execute the request
            var queryResponse = client.Execute(request);
            if ((queryResponse.ResponseStatus == ResponseStatus.Completed) &&
                    (queryResponse.StatusCode == HttpStatusCode.OK))
            {
                JObject parsed = JObject.Parse(queryResponse.Content);
                var parsedQueryResponse = parsed["QueryResponse"];
                if (parsedQueryResponse != null)
                {
                    var parsedEmployees = parsedQueryResponse["Employee"];
                    if (parsedEmployees != null)
                    {
                        foreach (var employee in parsedEmployees.Children())
                        {
                            var displayName = employee["DisplayName"].ToString();
                            var id = employee["Id"].ToString();

                            // Update id of employee in brizbee list
                            var ix = brzEmployees.FindIndex(x => x.DisplayName == displayName);
                            if (ix >= 0)
                            {
                                brzEmployees[ix] = new Serialization.QBO.Employee()
                                {
                                    DisplayName = displayName,
                                    Id = id
                                };
                            }

                            // Add employee to quickbooks online list
                            qboEmployees.Add(new Serialization.QBO.Employee()
                            {
                                DisplayName = displayName,
                                Id = id
                            });
                        }
                    }
                }
            }
            else
            {
                // Do something with the error
                throw new Exception(string.Format("Could not query QuickBooks Online for employees: {0}", queryResponse.Content));
            }

            // Compare the result with the employees from our database
            var missing = brzEmployees.Select(x => x.DisplayName).ToList().Except(qboEmployees.Select(x => x.DisplayName).ToList());
            if (missing.Any())
            {
                var joined = string.Join(", ", missing);
                throw new Exception(string.Format("Missing employees in QuickBooks Online: {0}", joined));
            }

            return qboEmployees;
        }

        private List<Serialization.QBO.Customer> VerifyCustomersExist(List<Punch> punches, string realmId, RestClient client)
        {
            // Get all the customer names to query QBO
            var brzCustomers = punches
                .GroupBy(p => p.Task.Job.QuickBooksCustomerJob)
                .Select(g => new Serialization.QBO.Customer()
                {
                    DisplayName = g.Key,
                    Id = "" // Unknown at the moment
                })
                .ToList();
            var qboCustomers = new List<Serialization.QBO.Customer>();

            // Build request to query customers
            var criteria = string.Join(",", brzCustomers.Select(x => string.Format("'{0}'", x.DisplayName)).ToArray());
            var query = string.Format("SELECT * FROM Customer WHERE DisplayName IN ({0})", criteria);
            var requestUrl = string.Format("/v3/company/{0}/query", realmId);
            var request = new RestRequest(requestUrl, Method.GET);
            request.AddParameter("query", query, ParameterType.QueryString);
            request.AddHeader("Content-Type", "text/plain");

            // Execute the request
            var queryResponse = client.Execute(request);
            if ((queryResponse.ResponseStatus == ResponseStatus.Completed) &&
                    (queryResponse.StatusCode == HttpStatusCode.OK))
            {
                JObject parsed = JObject.Parse(queryResponse.Content);
                var parsedQueryResponse = parsed["QueryResponse"];
                if (parsedQueryResponse != null)
                {
                    var parsedCustomers = parsedQueryResponse["Customer"];
                    if (parsedCustomers != null)
                    {
                        foreach (var customer in parsedCustomers.Children())
                        {
                            var displayName = customer["DisplayName"].ToString();
                            var id = customer["Id"].ToString();

                            // Update id of customer in brizbee list
                            var ix = brzCustomers.FindIndex(x => x.DisplayName == displayName);
                            if (ix >= 0)
                            {
                                brzCustomers[ix] = new Serialization.QBO.Customer()
                                {
                                    DisplayName = displayName,
                                    Id = id
                                };
                            }

                            // Add customer to quickbooks online list
                            qboCustomers.Add(new Serialization.QBO.Customer()
                            {
                                DisplayName = displayName,
                                Id = id
                            });
                        }
                    }
                }
            }
            else
            {
                // Do something with the error
                throw new Exception(string.Format("Could not query QuickBooks Online for customers: {0}", queryResponse.Content));
            }

            // Compare the result with the customers from our database
            var missing = brzCustomers.Select(x => x.DisplayName).ToList().Except(qboCustomers.Select(x => x.DisplayName).ToList());
            if (missing.Any())
            {
                var joined = string.Join(", ", missing);
                throw new Exception(string.Format("Missing customers in QuickBooks Online: {0}", joined));
            }

            return qboCustomers;
        }

        private List<Serialization.QBO.ServiceItem> VerifyServiceItemsExist(List<Punch> punches, string realmId, RestClient client)
        {
            // Get all the service item names to query QBO
            var brzServiceItems = punches
                .GroupBy(p => p.Task.QuickBooksServiceItem)
                .Select(g => new Serialization.QBO.ServiceItem()
                {
                    Name = g.Key,
                    Id = "" // Unknown at the moment
                })
                .ToList();
            var qboServiceItems = new List<Serialization.QBO.ServiceItem>();

            // Build request to query service items
            var criteria = string.Join(",", brzServiceItems.Select(x => string.Format("'{0}'", x.Name)).ToArray());
            var query = string.Format("SELECT * FROM Item WHERE Type = 'Service' AND Name IN ({0})", criteria);
            var requestUrl = string.Format("/v3/company/{0}/query", realmId);
            var request = new RestRequest(requestUrl, Method.GET);
            request.AddParameter("query", query, ParameterType.QueryString);
            request.AddHeader("Content-Type", "text/plain");

            // Execute the request
            var queryResponse = client.Execute(request);
            if ((queryResponse.ResponseStatus == ResponseStatus.Completed) &&
                    (queryResponse.StatusCode == HttpStatusCode.OK))
            {
                JObject parsed = JObject.Parse(queryResponse.Content);
                var parsedQueryResponse = parsed["QueryResponse"];
                if (parsedQueryResponse != null)
                {
                    var parsedCustomers = parsedQueryResponse["Item"];
                    if (parsedCustomers != null)
                    {
                        foreach (var customer in parsedCustomers.Children())
                        {
                            var name = customer["Name"].ToString();
                            var id = customer["Id"].ToString();

                            // Update id of customer in brizbee list
                            var ix = brzServiceItems.FindIndex(x => x.Name == name);
                            if (ix >= 0)
                            {
                                brzServiceItems[ix] = new Serialization.QBO.ServiceItem()
                                {
                                    Name = name,
                                    Id = id
                                };
                            }

                            // Add customer to quickbooks online list
                            qboServiceItems.Add(new Serialization.QBO.ServiceItem()
                            {
                                Name = name,
                                Id = id
                            });
                        }
                    }
                }
            }
            else
            {
                // Do something with the error
                throw new Exception(string.Format("Could not query QuickBooks Online for service items: {0}", queryResponse.Content));
            }

            // Compare the result with the service items from our database
            var missing = brzServiceItems.Select(x => x.Name).ToList().Except(qboServiceItems.Select(x => x.Name).ToList());
            if (missing.Any())
            {
                var joined = string.Join(", ", missing);
                throw new Exception(string.Format("Missing service items in QuickBooks Online: {0}", joined));
            }

            return qboServiceItems;
        }
    }
}