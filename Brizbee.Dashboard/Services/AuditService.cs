using Brizbee.Dashboard.Models;
using Brizbee.Dashboard.Security;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Brizbee.Dashboard.Services
{
    public class AuditService
    {
        public ApiService _apiService;
        private JsonSerializerOptions options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };

        public AuditService(ApiService apiService)
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

        public async Task<(List<Audit>, long?)> GetAuditsAsync(DateTime min, DateTime max, int pageSize = 100, int skip = 0, string sortBy = "Audits/CreatedAt", string sortDirection = "ASC", int[] userIds = null)
        {
            var filterParameters = new StringBuilder();

            if (userIds != null)
                filterParameters.Append(string.Join("", userIds.Select(x => $"&userIds={x}")));

            var response = await _apiService.GetHttpClient().GetAsync($"api/Audits?pageSize={pageSize}&skip={skip}&min={min.ToString("yyyy-MM-ddTHH:mm:ssZ")}&max={max.ToString("yyyy-MM-ddTHH:mm:ssZ")}&orderBy={sortBy}&orderByDirection={sortDirection}{filterParameters}");

            if (!response.IsSuccessStatusCode)
                return (new List<Audit>(0), 0);

            using var responseContent = await response.Content.ReadAsStreamAsync();
            var value = await JsonSerializer.DeserializeAsync<List<Audit>>(responseContent, options);
            var total = long.Parse(response.Headers.GetValues("X-Paging-TotalRecordCount").FirstOrDefault());
            return (value, total);
        }

        public async Task<(List<Audit>, long?)> GetPunchAuditsAsync(DateTime min, DateTime max, int pageSize = 100, int skip = 0, string sortBy = "Audits/CreatedAt", string sortDirection = "ASC", int[] userIds = null, int[] objectIds = null)
        {
            var filterParameters = new StringBuilder();

            if (userIds != null)
                filterParameters.Append(string.Join("", userIds.Select(x => $"&userIds={x}")));

            if (objectIds != null)
                filterParameters.Append(string.Join("", objectIds.Select(x => $"&objectIds={x}")));

            var response = await _apiService.GetHttpClient().GetAsync($"api/Audits/Punches?pageSize={pageSize}&skip={skip}&min={min.ToString("yyyy-MM-ddTHH:mm:ssZ")}&max={max.ToString("yyyy-MM-ddTHH:mm:ssZ")}&orderBy={sortBy}&orderByDirection={sortDirection}{filterParameters}");

            if (!response.IsSuccessStatusCode)
                return (new List<Audit>(0), 0);

            using var responseContent = await response.Content.ReadAsStreamAsync();
            var value = await JsonSerializer.DeserializeAsync<List<Audit>>(responseContent, options);
            var total = long.Parse(response.Headers.GetValues("X-Paging-TotalRecordCount").FirstOrDefault());
            return (value, total);
        }

        public async Task<(List<Audit>, long?)> GetTimeCardAuditsAsync(DateTime min, DateTime max, int pageSize = 100, int skip = 0, string sortBy = "Audits/CreatedAt", string sortDirection = "ASC", int[] userIds = null, int[] objectIds = null)
        {
            var filterParameters = new StringBuilder();

            if (userIds != null)
                filterParameters.Append(string.Join("", userIds.Select(x => $"&userIds={x}")));

            if (objectIds != null)
                filterParameters.Append(string.Join("", objectIds.Select(x => $"&objectIds={x}")));

            var response = await _apiService.GetHttpClient().GetAsync($"api/Audits/TimeCards?pageSize={pageSize}&skip={skip}&min={min.ToString("yyyy-MM-ddTHH:mm:ssZ")}&max={max.ToString("yyyy-MM-ddTHH:mm:ssZ")}&orderBy={sortBy}&orderByDirection={sortDirection}{filterParameters}");

            if (!response.IsSuccessStatusCode)
                return (new List<Audit>(0), 0);

            using var responseContent = await response.Content.ReadAsStreamAsync();
            var value = await JsonSerializer.DeserializeAsync<List<Audit>>(responseContent, options);
            var total = long.Parse(response.Headers.GetValues("X-Paging-TotalRecordCount").FirstOrDefault());
            return (value, total);
        }
    }
}
