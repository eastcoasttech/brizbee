using Brizbee.Blazor;
using Brizbee.Dashboard.Models;
using Brizbee.Dashboard.Security;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace Brizbee.Dashboard.Services
{
    public class ExportService
    {
        public ApiService _apiService;
        private JsonSerializerOptions options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };

        public ExportService(ApiService apiService)
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

        public async Task<(List<QuickBooksDesktopExport>, long?)> GetExportsAsync(int pageSize = 20, int skip = 0, string sortBy = "CreatedAt", string sortDirection = "ASC")
        {
            var response = await _apiService.GetHttpClient().GetAsync($"odata/QuickBooksDesktopExports?$count=true&$expand=Commit,User&$top={pageSize}&$skip={skip}&$orderby={sortBy} {sortDirection}");
            response.EnsureSuccessStatusCode();

            using var responseContent = await response.Content.ReadAsStreamAsync();
            var odataResponse = await JsonSerializer.DeserializeAsync<ODataListResponse<QuickBooksDesktopExport>>(responseContent, options);
            return (odataResponse.Value.ToList(), odataResponse.Count);
        }
    }
}
