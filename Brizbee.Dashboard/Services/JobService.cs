using Brizbee.Blazor;
using Brizbee.Common.Models;
using Brizbee.Common.Security;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

namespace Brizbee.Dashboard.Services
{
    public class JobService
    {
        public ApiService _apiService;
        private JsonSerializerOptions options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };

        public JobService(ApiService apiService)
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

        public async System.Threading.Tasks.Task<List<Job>> GetJobsAsync(int customerId, int pageSize = 100, int skip = 0, string sortBy = "Number", string sortDirection = "ASC")
        {
            var response = await _apiService.GetHttpClient().GetAsync($"odata/Jobs?$count=true&$top={pageSize}&$skip={skip}&$orderby={sortBy} {sortDirection}&$filter=CustomerId eq {customerId}");
            response.EnsureSuccessStatusCode();

            using var responseContent = await response.Content.ReadAsStreamAsync();
            var odataResponse = await JsonSerializer.DeserializeAsync<ODataResponse<Job>>(responseContent, options);
            return odataResponse.Value.ToList();
        }

        public async System.Threading.Tasks.Task<Job> GetJobByIdAsync(int id)
        {
            var response = await _apiService.GetHttpClient().GetAsync($"odata/Jobs({id})");
            response.EnsureSuccessStatusCode();

            using var responseContent = await response.Content.ReadAsStreamAsync();
            return await JsonSerializer.DeserializeAsync<Job>(responseContent);
        }
    }
}
