using Brizbee.Core.Models.Accounting;
using System.Text;
using System.Text.Json;

namespace Brizbee.Dashboard.Server.Services
{
    public class AccountService
    {
        public ApiService ApiService;
        private readonly JsonSerializerOptions _options = new()
        {
            PropertyNameCaseInsensitive = true
        };

        public AccountService(ApiService apiService)
        {
            ApiService = apiService;
        }
        
        public void ConfigureHeadersWithToken(string token)
        {
            // Clear old headers first
            ResetHeaders();

            ApiService.GetHttpClient().DefaultRequestHeaders.Add("Authorization", $"Bearer {token}");
        }

        public void ResetHeaders()
        {
            ApiService.GetHttpClient().DefaultRequestHeaders.Remove("Authorization");
        }

        public async Task<(List<Account>, long?)> GetAccountsAsync(int pageSize = 100, int skip = 0, string sortBy = "Accounts/Number", string sortDirection = "ASC", int[] objectIds = null)
        {
            var filterParameters = new StringBuilder();

            if (objectIds != null)
                filterParameters.Append(string.Join("", objectIds.Select(x => $"&objectIds={x}")));

            var response = await ApiService.GetHttpClient().GetAsync($"api/Accounting/Accounts?pageSize={pageSize}&skip={skip}&orderBy={sortBy}&orderByDirection={sortDirection}{filterParameters}");

            if (!response.IsSuccessStatusCode)
                return (new List<Account>(0), 0);

            await using var responseContent = await response.Content.ReadAsStreamAsync();
            var value = await JsonSerializer.DeserializeAsync<List<Account>>(responseContent, _options);
            var total = long.Parse(response.Headers.GetValues("X-Paging-TotalRecordCount").FirstOrDefault());
            return (value, total);
        }
    }
}
