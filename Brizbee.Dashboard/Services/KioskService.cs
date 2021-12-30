using Brizbee.Dashboard.Models;
using Brizbee.Dashboard.Security;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Brizbee.Dashboard.Services
{
    public class KioskService
    {
        public ApiService _apiService;
        private JsonSerializerOptions options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };

        public KioskService(ApiService apiService)
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

        public async Task<bool> PunchInAsync(int taskId, string latitude, string longitude, string browserName, string browserVersion, string operationSystemName, string operationSystemVersion, string timeZone)
        {
            // Build the URL with query parameters.
            var url = new StringBuilder();
            url.Append("api/Kiosk/PunchIn?");
            url.Append($"taskId={taskId}&");
            url.Append($"timeZone={timeZone}&");
            url.Append($"latitude={latitude}&");
            url.Append($"longitude={longitude}&");
            url.Append("sourceHardware=Web&");
            url.Append($"sourceOperatingSystem={operationSystemName}&");
            url.Append($"sourceOperatingSystemVersion={operationSystemVersion}&");
            url.Append($"sourceBrowser={browserName}&");
            url.Append($"sourceBrowserVersion={browserVersion}");

            using (var request = new HttpRequestMessage(HttpMethod.Post, url.ToString()))
            {
                using (var response = await _apiService
                    .GetHttpClient()
                    .SendAsync(request, HttpCompletionOption.ResponseHeadersRead)
                    .ConfigureAwait(false))
                {
                    if (response.IsSuccessStatusCode)
                        return true;
                    else
                        return false;
                }
            }
        }

        public async Task<bool> PunchOutAsync(string latitude, string longitude, string browserName, string browserVersion, string operationSystemName, string operationSystemVersion, string timeZone)
        {
            // Build the URL with query parameters.
            var url = new StringBuilder();
            url.Append("api/Kiosk/PunchOut?");
            url.Append($"timeZone={timeZone}&");
            url.Append($"latitude={latitude}&");
            url.Append($"longitude={longitude}&");
            url.Append("sourceHardware=Web&");
            url.Append($"sourceOperatingSystem={operationSystemName}&");
            url.Append($"sourceOperatingSystemVersion={operationSystemVersion}&");
            url.Append($"sourceBrowser={browserName}&");
            url.Append($"sourceBrowserVersion={browserVersion}");

            using (var request = new HttpRequestMessage(HttpMethod.Post, url.ToString()))
            {
                using (var response = await _apiService
                        .GetHttpClient()
                        .SendAsync(request, HttpCompletionOption.ResponseHeadersRead)
                        .ConfigureAwait(false))
                {
                    if (response.IsSuccessStatusCode)
                        return true;
                    else
                        return false;
                }
            }
        }

        public async Task<Punch> GetCurrentPunchAsync()
        {
            var response = await _apiService.GetHttpClient().GetAsync("api/Kiosk/Punches/Current");

            if (!response.IsSuccessStatusCode)
                return null;

            using var responseContent = await response.Content.ReadAsStreamAsync();
            var value = await JsonSerializer.DeserializeAsync<Punch>(responseContent, options);
            return value;
        }

        public async Task<bool> AddTimeCardAsync(DateTime enteredAt, int minutes, string notes, int taskId)
        {
            // Build the URL with query parameters.
            var url = new StringBuilder();
            url.Append("api/Kiosk/TimeCard?");
            url.Append($"taskId={taskId}&");
            url.Append($"enteredAt={enteredAt}&");
            url.Append($"minutes={minutes}&");

            if (!string.IsNullOrEmpty(notes))
                url.Append($"notes={notes}");

            using (var request = new HttpRequestMessage(HttpMethod.Post, url.ToString()))
            {
                using (var response = await _apiService
                        .GetHttpClient()
                        .SendAsync(request, HttpCompletionOption.ResponseHeadersRead)
                        .ConfigureAwait(false))
                {
                    if (response.IsSuccessStatusCode)
                        return true;
                    else
                        return false;
                }
            }
        }

        public async Task<Brizbee.Dashboard.Models.Task> SearchTasksAsync(string taskNumber)
        {
            var response = await _apiService.GetHttpClient().GetAsync($"api/Kiosk/SearchTasks?taskNumber={taskNumber}");

            if (response.IsSuccessStatusCode)
            {
                using var responseContent = await response.Content.ReadAsStreamAsync();
                var value = await JsonSerializer.DeserializeAsync<Brizbee.Dashboard.Models.Task>(responseContent, options);
                return value;
            }
            else
            {
                return null;
            }
        }

        public async Task<List<Customer>> GetCustomersAsync()
        {
            var response = await _apiService.GetHttpClient().GetAsync("api/Kiosk/Customers");

            if (!response.IsSuccessStatusCode)
                return new List<Customer>(0);

            using var responseContent = await response.Content.ReadAsStreamAsync();
            var value = await JsonSerializer.DeserializeAsync<List<Customer>>(responseContent, options);
            return value;
        }

        public async Task<List<Job>> GetProjectsAsync(int customerId)
        {
            var response = await _apiService.GetHttpClient().GetAsync($"api/Kiosk/Projects?customerId={customerId}");

            if (!response.IsSuccessStatusCode)
                return new List<Job>(0);

            using var responseContent = await response.Content.ReadAsStreamAsync();
            var value = await JsonSerializer.DeserializeAsync<List<Job>>(responseContent, options);
            return value;
        }

        public async Task<List<Brizbee.Dashboard.Models.Task>> GetTasksAsync(int projectId)
        {
            var response = await _apiService.GetHttpClient().GetAsync($"api/Kiosk/Tasks?projectId={projectId}");

            if (!response.IsSuccessStatusCode)
                return new List<Brizbee.Dashboard.Models.Task>(0);

            using var responseContent = await response.Content.ReadAsStreamAsync();
            var value = await JsonSerializer.DeserializeAsync<List<Brizbee.Dashboard.Models.Task>>(responseContent, options);
            return value;
        }
    }
}
