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

        public async Task<(List<TimesheetEntry>, long?)> GetPunchesAsync(DateTime min, DateTime max, int pageSize = 100, int skip = 0, string sortBy = "InAt", string sortDirection = "ASC")
        {
            var response = await _apiService.GetHttpClient().GetAsync($"odata/TimesheetEntries?$count=true&$expand=User,Task($expand=Job($expand=Customer))&$top={pageSize}&$skip={skip}&$filter=EnteredAt ge {min.ToString("yyyy-MM-ddTHH:mm:ssZ")} and EnteredAt le {max.ToString("yyyy-MM-ddTHH:mm:ssZ")}&$orderby={sortBy} {sortDirection}");
            response.EnsureSuccessStatusCode();

            using var responseContent = await response.Content.ReadAsStreamAsync();
            var odataResponse = await JsonSerializer.DeserializeAsync<ODataResponse<TimesheetEntry>>(responseContent, options);
            return (odataResponse.Value.ToList(), odataResponse.Count);
        }

    }
}
