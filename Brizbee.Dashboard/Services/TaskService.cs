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
    public class TaskService
    {
        public ApiService _apiService;
        private JsonSerializerOptions options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };

        public TaskService(ApiService apiService)
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

        public async Task<(List<Brizbee.Common.Models.Task>, long?)> GetTasksAsync(int jobId, int pageSize = 100, int skip = 0, string sortBy = "Number", string sortDirection = "ASC")
        {
            var response = await _apiService.GetHttpClient().GetAsync($"odata/Tasks?$count=true&$top={pageSize}&$skip={skip}&$orderby={sortBy} {sortDirection}&$filter=JobId eq {jobId}");
            response.EnsureSuccessStatusCode();

            using var responseContent = await response.Content.ReadAsStreamAsync();
            var odataResponse = await JsonSerializer.DeserializeAsync<ODataListResponse<Brizbee.Common.Models.Task>>(responseContent, options);
            return (odataResponse.Value.ToList(), odataResponse.Count);
        }

        public async Task<Brizbee.Common.Models.Task> GetTaskByIdAsync(int id)
        {
            var response = await _apiService.GetHttpClient().GetAsync($"odata/Tasks({id})?$expand=BasePayrollRate,BaseServiceRate");
            response.EnsureSuccessStatusCode();

            using var responseContent = await response.Content.ReadAsStreamAsync();
            return await JsonSerializer.DeserializeAsync<Brizbee.Common.Models.Task>(responseContent);
        }

        public async Task<(List<Brizbee.Common.Models.Task>, long?)> GetTasksForPunchesAsync(DateTime min, DateTime max)
        {
            var response = await _apiService.GetHttpClient().GetAsync($"api/TasksExpanded/ForPunches?min={min.ToString("yyyy-MM-dd")}&max={max.ToString("yyyy-MM-dd")}");

            if (!response.IsSuccessStatusCode)
                return (new List<Brizbee.Common.Models.Task>(0), 0);

            using var responseContent = await response.Content.ReadAsStreamAsync();
            var value = await JsonSerializer.DeserializeAsync<List<Brizbee.Common.Models.Task>>(responseContent, options);
            var total = long.Parse(response.Headers.GetValues("X-Paging-TotalRecordCount").FirstOrDefault());
            return (value, total);
        }

        public async Task<Brizbee.Common.Models.Task> SearchTasksAsync(string number)
        {
            var response = await _apiService.GetHttpClient().GetAsync($"odata/Tasks/Search(Number='{number}')");

            if (response.IsSuccessStatusCode)
            {
                using var responseContent = await response.Content.ReadAsStreamAsync();
                var odataResponse = await JsonSerializer.DeserializeAsync<Brizbee.Common.Models.Task>(responseContent, options);
                return odataResponse;
            }
            else
            {
                return null;
            }
        }

        public async Task<bool> DeleteTaskAsync(int id)
        {
            var response = await _apiService.GetHttpClient().DeleteAsync($"odata/Tasks({id})");
            if (response.IsSuccessStatusCode)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public async Task<Brizbee.Common.Models.Task> SaveTaskAsync(Brizbee.Common.Models.Task task)
        {
            var url = task.Id != 0 ? $"odata/Tasks({task.Id})" : "odata/Tasks";
            var method = task.Id != 0 ? HttpMethod.Patch : HttpMethod.Post;

            using (var request = new HttpRequestMessage(method, url))
            {
                var payload = new Dictionary<string, object>() {
                    { "Name", task.Name },
                    { "Number", task.Number },
                    { "Group", task.Group },
                    { "BaseServiceRateId", task.BaseServiceRateId },
                    { "BasePayrollRateId", task.BasePayrollRateId },
                };

                // Can only be configured at creation.
                if (task.Id == 0)
                    payload.Add("JobId", task.JobId);

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
                                var deserialized = await JsonSerializer.DeserializeAsync<Brizbee.Common.Models.Task>(responseContent, options);
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

        public async Task<string> GetNextNumberAsync()
        {
            var response = await _apiService.GetHttpClient().PostAsync("odata/Tasks/NextNumber", new StringContent(""));
            response.EnsureSuccessStatusCode();

            using var responseContent = await response.Content.ReadAsStreamAsync();
            var odataResponse = await JsonSerializer.DeserializeAsync<ODataSingleResponse<string>>(responseContent, options);
            return odataResponse.Value;
        }
    }
}
