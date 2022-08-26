using Brizbee.Core.Models.Accounting;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Brizbee.Dashboard.Services
{
    public class AccountService
    {
        public ApiService _apiService;
        private JsonSerializerOptions options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };

        public AccountService(ApiService apiService)
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

        public async Task<(List<Account>, long?)> GetAccountsAsync(int pageSize = 100, int skip = 0, string sortBy = "Accounts/Number", string sortDirection = "ASC", int[] objectIds = null)
        {
            var filterParameters = new StringBuilder();

            if (objectIds != null)
                filterParameters.Append(string.Join("", objectIds.Select(x => $"&objectIds={x}")));

            var response = await _apiService.GetHttpClient().GetAsync($"api/Accounts?pageSize={pageSize}&skip={skip}&orderBy={sortBy}&orderByDirection={sortDirection}{filterParameters}");

            if (!response.IsSuccessStatusCode)
                return (new List<Account>(0), 0);

            using var responseContent = await response.Content.ReadAsStreamAsync();
            var value = await JsonSerializer.DeserializeAsync<List<Account>>(responseContent, options);
            var total = long.Parse(response.Headers.GetValues("X-Paging-TotalRecordCount").FirstOrDefault());
            return (value, total);
        }
    }
}
