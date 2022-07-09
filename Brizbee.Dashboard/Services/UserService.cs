using Brizbee.Blazor;
using Brizbee.Dashboard.Models;
using Brizbee.Dashboard.Security;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Brizbee.Dashboard.Services
{
    public class UserService
    {
        public ApiService _apiService;
        private JsonSerializerOptions options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };

        public UserService(ApiService apiService)
        {
            _apiService = apiService;
        }

        public void ConfigureHeadersWithToken(string token)
        {
            // Clear old headers first
            ResetHeaders();

            _apiService.GetHttpClient().DefaultRequestHeaders.Add("Authorization", $"Bearer {token}");
        }

        public void ResetHeaders()
        {
            _apiService.GetHttpClient().DefaultRequestHeaders.Remove("Authorization");
        }

        public async Task<(List<User>, long?)> GetUsersAsync(int pageSize = 100, int skip = 0, string sortBy = "Name", string sortDirection = "ASC")
        {
            var response = await _apiService.GetHttpClient().GetAsync($"odata/Users?$count=true&$top={pageSize}&$skip={skip}&$orderby={sortBy} {sortDirection}");
            response.EnsureSuccessStatusCode();

            using var responseContent = await response.Content.ReadAsStreamAsync();
            var odataResponse = await JsonSerializer.DeserializeAsync<ODataListResponse<User>>(responseContent, options);
            return (odataResponse.Value.ToList(), odataResponse.Count);
        }

        public async Task<User> GetUserByIdAsync(int id)
        {
            var response = await _apiService.GetHttpClient().GetAsync($"odata/Users({id})");
            response.EnsureSuccessStatusCode();

            using var responseContent = await response.Content.ReadAsStreamAsync();
            return await JsonSerializer.DeserializeAsync<User>(responseContent, options);
        }

        public async Task<List<User>> SearchUsersAsync(string query)
        {
            var response = await _apiService.GetHttpClient().GetAsync($"odata/Users?$filter=contains(Name,'{query}')&$select=EmailAddress,Name,Id");
            response.EnsureSuccessStatusCode();

            using var responseContent = await response.Content.ReadAsStreamAsync();
            var odataResponse = await JsonSerializer.DeserializeAsync<ODataListResponse<User>>(responseContent, options);
            return odataResponse.Value.ToList();
        }

        public async Task<bool> DeleteUserAsync(int id)
        {
            var response = await _apiService.GetHttpClient().DeleteAsync($"odata/Users({id})");
            if (response.IsSuccessStatusCode)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public async Task<User> SaveUserAsync(User user)
        {
            var url = user.Id != 0 ? $"odata/Users({user.Id})" : "odata/Users";
            var method = user.Id != 0 ? HttpMethod.Patch : HttpMethod.Post;

            using (var request = new HttpRequestMessage(method, url))
            {
                var payload = new Dictionary<string, object>() {
                    { "Name", user.Name },
                    { "TimeZone", user.TimeZone },
                    { "UsesTimesheets", user.UsesTimesheets },
                    { "UsesMobileClock", user.UsesMobileClock },
                    { "UsesTouchToneClock", user.UsesTouchToneClock },
                    { "UsesWebClock", user.UsesWebClock },
                    { "ShouldSendMidnightPunchEmail", user.ShouldSendMidnightPunchEmail },
                    { "AllowedPhoneNumbers", user.AllowedPhoneNumbers },
                    { "NotificationMobileNumbers", user.NotificationMobileNumbers },
                    { "Pin", user.Pin },
                    { "QuickBooksEmployee", user.QuickBooksEmployee },
                    { "RequiresLocation", user.RequiresLocation },
                    { "IsActive", user.IsActive },
                    { "CanViewCustomers", user.CanViewCustomers },
                    { "CanCreateCustomers", user.CanCreateCustomers },
                    { "CanModifyCustomers", user.CanModifyCustomers },
                    { "CanDeleteCustomers", user.CanDeleteCustomers },
                    { "CanViewProjects", user.CanViewProjects },
                    { "CanCreateProjects", user.CanCreateProjects },
                    { "CanModifyProjects", user.CanModifyProjects },
                    { "CanDeleteProjects", user.CanDeleteProjects },
                    { "CanMergeProjects", user.CanMergeProjects },
                    { "CanViewTasks", user.CanViewTasks },
                    { "CanCreateTasks", user.CanCreateTasks },
                    { "CanModifyTasks", user.CanModifyTasks },
                    { "CanDeleteTasks", user.CanDeleteTasks },
                    { "CanViewAudits", user.CanViewAudits },
                    { "CanViewRates", user.CanViewRates },
                    { "CanCreateRates", user.CanCreateRates },
                    { "CanModifyRates", user.CanModifyRates },
                    { "CanDeleteRates", user.CanDeleteRates },
                    { "CanViewPunches", user.CanViewPunches },
                    { "CanCreatePunches", user.CanCreatePunches },
                    { "CanModifyPunches", user.CanModifyPunches },
                    { "CanDeletePunches", user.CanDeletePunches },
                    { "CanViewTimecards", user.CanViewTimecards },
                    { "CanCreateTimecards", user.CanCreateTimecards },
                    { "CanModifyTimecards", user.CanModifyTimecards },
                    { "CanDeleteTimecards", user.CanDeleteTimecards },
                    { "CanViewUsers", user.CanViewUsers },
                    { "CanCreateUsers", user.CanCreateUsers },
                    { "CanModifyUsers", user.CanModifyUsers },
                    { "CanDeleteUsers", user.CanDeleteUsers },
                    { "CanViewOrganizationDetails", user.CanViewOrganizationDetails },
                    { "CanModifyOrganizationDetails", user.CanModifyOrganizationDetails },
                    { "CanViewReports", user.CanViewReports },
                    { "CanViewLocks", user.CanViewLocks },
                    { "CanCreateLocks", user.CanCreateLocks },
                    { "CanUndoLocks", user.CanUndoLocks },
                    { "CanViewInventoryItems", user.CanViewInventoryItems },
                    { "CanModifyInventoryItems", user.CanModifyInventoryItems },
                    { "CanSyncInventoryItems", user.CanSyncInventoryItems },
                    { "CanViewInventoryConsumptions", user.CanViewInventoryConsumptions },
                    { "CanDeleteInventoryConsumptions", user.CanDeleteInventoryConsumptions },
                    { "CanSyncInventoryConsumptions", user.CanSyncInventoryConsumptions },
                    { "Role", "Standard" }
                };

                if (user.Id == 0)
                    payload.Add("EmailAddress", user.EmailAddress);

                if (!string.IsNullOrEmpty(user.Password))
                    payload.Add("Password", user.Password);

                var json = JsonSerializer.Serialize(payload, options);

                using (var stringContent = new StringContent(json, Encoding.UTF8, "application/json"))
                {
                    request.Content = stringContent;

                    using (var response = await _apiService
                        .GetHttpClient()
                        .SendAsync(request, HttpCompletionOption.ResponseHeadersRead)
                        .ConfigureAwait(false))
                    {
                        if (response.IsSuccessStatusCode)
                        {
                            using var responseContent = await response.Content.ReadAsStreamAsync();

                            if (response.StatusCode == HttpStatusCode.NoContent)
                            {
                                return null;
                            }
                            else
                            {
                                var deserialized = await JsonSerializer.DeserializeAsync<User>(responseContent, options);
                                return deserialized;
                            }
                        }
                        else
                        {
                            return null;
                        }
                    }
                }
            }
        }

        public async Task<bool> SaveUserDetailsAsync(int userId, string timeZone, string emailAddress, string name, string pin, string password = null)
        {
            using (var request = new HttpRequestMessage(HttpMethod.Patch, $"odata/Users({userId})"))
            {
                var payload = new Dictionary<string, object>() {
                    { "TimeZone", timeZone },
                    { "Name", name },
                    { "Pin", pin }
                };

                if (!string.IsNullOrEmpty(password))
                    payload.Add("Password", password);

                var json = JsonSerializer.Serialize(payload, options);

                using (var stringContent = new StringContent(json, Encoding.UTF8, "application/json"))
                {
                    request.Content = stringContent;

                    using (var response = await _apiService
                        .GetHttpClient()
                        .SendAsync(request, HttpCompletionOption.ResponseHeadersRead)
                        .ConfigureAwait(false))
                    {
                        if (response.IsSuccessStatusCode)
                        {
                            return true;
                        }
                        else
                        {
                            return false;
                        }
                    }
                }
            }
        }

        public async Task<bool> RegisterAsync(Serialization.Registration registration)
        {
            using (var request = new HttpRequestMessage(HttpMethod.Post, $"api/Auth/Register"))
            {
                var payload = new {
                    Organization = new {
                        Name = registration.Organization,
                        PlanId = registration.PlanId
                    },
                    User = new {
                        Name = registration.Name,
                        EmailAddress = registration.EmailAddress,
                        Password = registration.Password,
                        TimeZone = "America/New_York"
                    }
                };

                var json = JsonSerializer.Serialize(payload, options);

                Console.WriteLine(json);

                using (var stringContent = new StringContent(json, Encoding.UTF8, "application/json"))
                {
                    request.Content = stringContent;

                    using (var response = await _apiService
                        .GetHttpClient()
                        .SendAsync(request, HttpCompletionOption.ResponseHeadersRead)
                        .ConfigureAwait(false))
                    {
                        if (response.IsSuccessStatusCode)
                        {
                            return true;
                        }
                        else
                        {
                            return false;
                        }
                    }
                }
            }
        }
    }
}
