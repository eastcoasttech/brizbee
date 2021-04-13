using Brizbee.Blazor;
using Brizbee.Common.Models;
using Brizbee.Common.Security;
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
    public class PunchService
    {
        public ApiService _apiService;
        private JsonSerializerOptions options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };

        public PunchService(ApiService apiService)
        {
            _apiService = apiService;
        }

        public void ConfigureHeadersWithCredentials(Credential credential)
        {
            // Clear old headers first
            ResetHeaders();

            _apiService.GetHttpClient().DefaultRequestHeaders.Add("AUTH_USER_ID", credential.AuthUserId);
            _apiService.GetHttpClient().DefaultRequestHeaders.Add("AUTH_TOKEN", credential.AuthToken);
            _apiService.GetHttpClient().DefaultRequestHeaders.Add("AUTH_EXPIRATION", credential.AuthExpiration);
        }

        public void ResetHeaders()
        {
            _apiService.GetHttpClient().DefaultRequestHeaders.Remove("AUTH_USER_ID");
            _apiService.GetHttpClient().DefaultRequestHeaders.Remove("AUTH_TOKEN");
            _apiService.GetHttpClient().DefaultRequestHeaders.Remove("AUTH_EXPIRATION");
        }

        public async Task<(List<Punch>, long?)> GetPunchesAsync(DateTime min, DateTime max, int pageSize = 100, int skip = 0, string sortBy = "InAt", string sortDirection = "ASC")
        {
            var response = await _apiService.GetHttpClient().GetAsync($"odata/Punches?$count=true&$expand=User,ServiceRate,PayrollRate,Task($expand=Job($expand=Customer))&$top={pageSize}&$skip={skip}&$filter=InAt ge {min.ToString("yyyy-MM-ddTHH:mm:ssZ")} and InAt le {max.ToString("yyyy-MM-ddTHH:mm:ssZ")}&$orderby={sortBy} {sortDirection}");
            response.EnsureSuccessStatusCode();

            using var responseContent = await response.Content.ReadAsStreamAsync();
            var odataResponse = await JsonSerializer.DeserializeAsync<ODataListResponse<Punch>>(responseContent, options);
            return (odataResponse.Value.ToList(), odataResponse.Count);
        }

        public async Task<(List<Punch>, long?)> GetExpandedPunchesAsync(DateTime min, DateTime max, int pageSize = 100, int skip = 0, string sortBy = "InAt", string sortDirection = "ASC")
        {
            var response = await _apiService.GetHttpClient().GetAsync($"api/PunchesExpanded?pageSize={pageSize}&skip={skip}&min={min.ToString("yyyy-MM-ddTHH:mm:ssZ")}&max={max.ToString("yyyy-MM-ddTHH:mm:ssZ")}&orderBy={sortBy}&orderByDirection={sortDirection}");
            response.EnsureSuccessStatusCode();

            using var responseContent = await response.Content.ReadAsStreamAsync();
            var value = await JsonSerializer.DeserializeAsync<List<Punch>>(responseContent, options);
            var total = long.Parse(response.Headers.GetValues("X-Paging-TotalRecordCount").FirstOrDefault());
            return (value, total);
        }

        public async Task<Punch> GetPunchByIdAsync(int id)
        {
            var response = await _apiService.GetHttpClient().GetAsync($"odata/Punches({id})?$expand=Task($expand=Job)");
            response.EnsureSuccessStatusCode();

            using var responseContent = await response.Content.ReadAsStreamAsync();
            return await JsonSerializer.DeserializeAsync<Punch>(responseContent, options);
        }

        public async Task<Punch> GetCurrentAsync()
        {
            var response = await _apiService.GetHttpClient().GetAsync($"odata/Punches/Default.Current?$expand=Task($expand=Job($expand=Customer))");
            response.EnsureSuccessStatusCode();

            using var responseContent = await response.Content.ReadAsStreamAsync();
            var odataResponse = await JsonSerializer.DeserializeAsync<ODataListResponse<Punch>>(responseContent, options);
            var punches = odataResponse.Value.ToList();
            return punches.FirstOrDefault();
        }

        public async Task<bool> DeletePunchAsync(int id)
        {
            var response = await _apiService.GetHttpClient().DeleteAsync($"odata/Punches({id})");
            if (response.IsSuccessStatusCode)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public async Task<Punch> SavePunchAsync(Punch punch)
        {
            var url = punch.Id != 0 ? $"odata/Punches({punch.Id})" : "odata/Punches";
            var method = punch.Id != 0 ? HttpMethod.Patch : HttpMethod.Post;

            using (var request = new HttpRequestMessage(method, url))
            {
                // Platform detection
                var browserName = "Unknown"; // 'Safari'
                var browserVersion = "Unknown"; // '5.1'
                var operatingSystem = "Unknown"; // 'iOS'
                var operatingSystemVersion = "Unknown"; // 5.0

                var payload = new Dictionary<string, object>() {
                    { "InAt", punch.InAt.ToString("yyyy-MM-ddTHH:mm:00Z") },
                    { "InAtTimeZone", punch.InAtTimeZone },
                    { "TaskId", punch.TaskId },
                    { "UserId", punch.UserId }
                };

                // Only new punches have source details.
                if (punch.Id == 0)
                {
                    payload.Add("InAtSourceHardware", "Dashboard");
                    payload.Add("InAtSourceOperatingSystem", operatingSystem);
                    payload.Add("InAtSourceOperatingSystemVersion", operatingSystemVersion);
                    payload.Add("InAtSourceBrowser", browserName);
                    payload.Add("InAtSourceBrowserVersion", browserVersion);
                }

                // OutAt is optional when editing manually
                if (punch.OutAt.HasValue)
                {
                    payload.Add("OutAt", punch.OutAt.Value.ToString("yyyy-MM-ddTHH:mm:00Z"));
                    payload.Add("OutAtTimeZone", punch.OutAtTimeZone);

                    // Only new punches have source details.
                    if (punch.Id == 0)
                    {
                        payload.Add("OutAtSourceHardware", "Dashboard");
                        payload.Add("OutAtSourceOperatingSystem", operatingSystem);
                        payload.Add("OutAtSourceOperatingSystemVersion", operatingSystemVersion);
                        payload.Add("OutAtSourceBrowser", browserName);
                        payload.Add("OutAtSourceBrowserVersion", browserVersion);
                    }
                }

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
                                var deserialized = await JsonSerializer.DeserializeAsync<Punch>(responseContent, options);
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

        public async Task<bool> SplitPunches(DateTime inAt, DateTime outAt)
        {
            using (var request = new HttpRequestMessage(HttpMethod.Post, "odata/Punches/Default.SplitAtMidnight"))
            {
                var payload = new Dictionary<string, object>() {
                    { "InAt", new DateTime(inAt.Year, inAt.Month, inAt.Day, 0, 0, 0).ToString("yyyy-MM-ddTHH:mm:00Z") },
                    { "OutAt", new DateTime(outAt.Year, outAt.Month, outAt.Day, 23, 59, 59).ToString("yyyy-MM-ddTHH:mm:00Z") }
                };

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

        public async Task<bool> LockPunches(DateTime inAt, DateTime outAt)
        {
            using (var request = new HttpRequestMessage(HttpMethod.Post, "odata/Commits"))
            {
                var payload = new Dictionary<string, object>() {
                    { "InAt", inAt.ToString("yyyy-MM-dd") },
                    { "OutAt", outAt.ToString("yyyy-MM-dd") }
                };

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

        public async Task<bool> PostPopulateRatesAsync(DateTime inAt, DateTime outAt, List<Dictionary<string, object>> populateRateOptions)
        {
            using (var request = new HttpRequestMessage(HttpMethod.Post, "odata/Punches/Default.PopulateRates"))
            {
                var payload = new
                {
                    Options = new Dictionary<string, object>() {
                        { "InAt", inAt.ToString("yyyy-MM-dd") },
                        { "OutAt", outAt.ToString("yyyy-MM-dd") },
                        { "Options", populateRateOptions }
                    }
                };

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

        public async Task<bool> PunchIn(int taskId, string latitude, string longitude, string timeZone)
        {
            using (var request = new HttpRequestMessage(HttpMethod.Post, "odata/Punches/Default.PunchIn"))
            {
                var payload = new Dictionary<string, object>() {
                    { "InAtTimeZone", timeZone },
                    { "LatitudeForInAt", latitude },
                    { "LongitudeForInAt", longitude },
                    { "TaskId", taskId },
                    { "SourceHardware", "Web" },
                    { "SourceOperatingSystem", "Unknown" },
                    { "SourceOperatingSystemVersion", "Unknown" },
                    { "SourceBrowser", "Unknown" },
                    { "SourceBrowserVersion", "Unknown" }
                };

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

        public async Task<bool> PunchOut(string latitude, string longitude, string timeZone)
        {
            using (var request = new HttpRequestMessage(HttpMethod.Post, "odata/Punches/Default.PunchOut"))
            {
                var payload = new Dictionary<string, object>() {
                    { "OutAtTimeZone", timeZone },
                    { "LatitudeForOutAt", latitude },
                    { "LongitudeForOutAt", longitude },
                    { "SourceHardware", "Web" },
                    { "SourceOperatingSystem", "Unknown" },
                    { "SourceOperatingSystemVersion", "Unknown" },
                    { "SourceBrowser", "Unknown" },
                    { "SourceBrowserVersion", "Unknown" }
                };

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
    }
}
