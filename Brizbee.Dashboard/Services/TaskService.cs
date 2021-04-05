using Brizbee.Blazor;
using Brizbee.Common.Models;
using Brizbee.Common.Security;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

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

        public async System.Threading.Tasks.Task<(List<Task>, long?)> GetTasksAsync(int jobId, int pageSize = 100, int skip = 0, string sortBy = "Number", string sortDirection = "ASC")
        {
            var response = await _apiService.GetHttpClient().GetAsync($"odata/Tasks?$count=true&$top={pageSize}&$skip={skip}&$orderby={sortBy} {sortDirection}&$filter=JobId eq {jobId}");
            response.EnsureSuccessStatusCode();

            using var responseContent = await response.Content.ReadAsStreamAsync();
            var odataResponse = await JsonSerializer.DeserializeAsync<ODataResponse<Task>>(responseContent, options);
            return (odataResponse.Value.ToList(), odataResponse.Count);
        }

        public async System.Threading.Tasks.Task<Task> GetTaskByIdAsync(int id)
        {
            var response = await _apiService.GetHttpClient().GetAsync($"odata/Tasks({id})");
            response.EnsureSuccessStatusCode();

            using var responseContent = await response.Content.ReadAsStreamAsync();
            return await JsonSerializer.DeserializeAsync<Task>(responseContent);
        }

        public async System.Threading.Tasks.Task<List<Task>> GetTasksForPunchesAsync(DateTime min, DateTime max)
        {
            var response = await _apiService.GetHttpClient().GetAsync($"odata/Tasks/Default.ForPunches(InAt='{min.ToString("yyyy-MM-dd")}',OutAt='{max.ToString("yyyy-MM-dd")}')?$count=true&$expand=BasePayrollRate,BaseServiceRate,Job($expand=Customer)");
            response.EnsureSuccessStatusCode();

            using var responseContent = await response.Content.ReadAsStreamAsync();
            var odataResponse = await JsonSerializer.DeserializeAsync<ODataResponse<Task>>(responseContent, options);
            return odataResponse.Value.ToList();
        }
    }
}
