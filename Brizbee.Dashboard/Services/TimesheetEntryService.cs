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
    public class TimesheetEntryService
    {
        public ApiService _apiService;
        private JsonSerializerOptions options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };

        public TimesheetEntryService(ApiService apiService)
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

        public async Task<(List<TimesheetEntry>, long?)> GetTimesheetEntriesAsync(DateTime min, DateTime max, int pageSize = 100, int skip = 0, string sortBy = "InAt", string sortDirection = "ASC")
        {
            var response = await _apiService.GetHttpClient().GetAsync($"odata/TimesheetEntries?$count=true&$expand=User,Task($expand=Job($expand=Customer))&$top={pageSize}&$skip={skip}&$filter=EnteredAt ge {min.ToString("yyyy-MM-ddTHH:mm:ssZ")} and EnteredAt le {max.ToString("yyyy-MM-ddTHH:mm:ssZ")}&$orderby={sortBy} {sortDirection}");
            response.EnsureSuccessStatusCode();

            using var responseContent = await response.Content.ReadAsStreamAsync();
            var odataResponse = await JsonSerializer.DeserializeAsync<ODataListResponse<TimesheetEntry>>(responseContent, options);
            return (odataResponse.Value.ToList(), odataResponse.Count);
        }

        public async Task<TimesheetEntry> GetTimesheetEntryByIdAsync(int id)
        {
            var response = await _apiService.GetHttpClient().GetAsync($"odata/TimesheetEntries({id})?$expand=Task($expand=Job)");
            response.EnsureSuccessStatusCode();

            using var responseContent = await response.Content.ReadAsStreamAsync();
            return await JsonSerializer.DeserializeAsync<TimesheetEntry>(responseContent, options);
        }

        public async Task<bool> DeleteTimesheetEntryAsync(int id)
        {
            var response = await _apiService.GetHttpClient().DeleteAsync($"odata/TimesheetEntries({id})");
            if (response.IsSuccessStatusCode)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public async Task<TimesheetEntry> SaveTimesheetEntryAsync(TimesheetEntry timesheetEntry)
        {
            var url = timesheetEntry.Id != 0 ? $"odata/TimesheetEntries({timesheetEntry.Id})" : "odata/TimesheetEntries";
            var method = timesheetEntry.Id != 0 ? HttpMethod.Patch : HttpMethod.Post;

            using (var request = new HttpRequestMessage(method, url))
            {
                var payload = new Dictionary<string, object>() {
                    { "EnteredAt", timesheetEntry.EnteredAt.ToString("yyyy-MM-ddT00:00:00Z") },
                    { "Minutes", timesheetEntry.Minutes },
                    { "Notes", timesheetEntry.Notes },
                    { "TaskId", timesheetEntry.TaskId },
                    { "UserId", timesheetEntry.UserId }
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
                            using var responseContent = await response.Content.ReadAsStreamAsync();

                            if (response.StatusCode == HttpStatusCode.NoContent)
                            {
                                return null;
                            }
                            else
                            {
                                var deserialized = await JsonSerializer.DeserializeAsync<TimesheetEntry>(responseContent, options);
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
    }
}
