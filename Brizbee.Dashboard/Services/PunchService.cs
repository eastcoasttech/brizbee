using Brizbee.Blazor;
using Brizbee.Common.Models;
using Brizbee.Common.Security;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace Brizbee.Dashboard.Services
{
    public class PunchService
    {
        public ApiService _apiService;
        private JsonSerializerOptions options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
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
            var response = await _apiService.GetHttpClient().GetAsync($"odata/Punches?$count=true&$expand=User,ServiceRate,PayrollRate,Task($expand=Job($expand=Customer))&$top={pageSize}&$skip={skip}&$filter=InAt ge {min.ToString("yyyy-MM-ddTHH:mm:ss-00:00")} and InAt le {max.ToString("yyyy-MM-ddTHH:mm:ss-00:00")}&$orderby={sortBy} {sortDirection}");
            response.EnsureSuccessStatusCode();

            using var responseContent = await response.Content.ReadAsStreamAsync();
            var odataResponse = await JsonSerializer.DeserializeAsync<ODataResponse<Punch>>(responseContent, options);
            return (odataResponse.Value.ToList(), odataResponse.Count);
        }

        public async Task<Punch> GetPunchByIdAsync(int id)
        {
            var response = await _apiService.GetHttpClient().GetAsync($"odata/Punches({id})");
            response.EnsureSuccessStatusCode();

            using var responseContent = await response.Content.ReadAsStreamAsync();
            return await JsonSerializer.DeserializeAsync<Punch>(responseContent, options);
        }

        public async Task<Punch> GetCurrentAsync()
        {
            var response = await _apiService.GetHttpClient().GetAsync($"odata/Punches/Default.Current?$expand=Task($expand=Job($expand=Customer))");
            response.EnsureSuccessStatusCode();

            using var responseContent = await response.Content.ReadAsStreamAsync();
            var odataResponse = await JsonSerializer.DeserializeAsync<ODataResponse<Punch>>(responseContent, options);
            var punches = odataResponse.Value.ToList();
            return punches.FirstOrDefault();
        }
    }
}
