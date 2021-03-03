using Brizbee.Blazor;
using Brizbee.Common.Models;
using Brizbee.Common.Security;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace Brizbee.Dashboard.Services
{
    public class CustomerService
    {
        public ApiService _apiService;
        private JsonSerializerOptions options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
        };

        public CustomerService(ApiService apiService)
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

        public async Task<List<Customer>> GetCustomersAsync(int pageSize = 100, int skip = 0, string sortBy = "Number", string sortDirection = "ASC")
        {
            var response = await _apiService.GetHttpClient().GetAsync($"odata/Customers?$count=true&$top={pageSize}&$skip={skip}&$orderby={sortBy} {sortDirection}");
            response.EnsureSuccessStatusCode();

            using var responseContent = await response.Content.ReadAsStreamAsync();
            var odataResponse = await JsonSerializer.DeserializeAsync<ODataResponse<Customer>>(responseContent, options);
            return odataResponse.Value.ToList();
        }

        public async Task<Customer> GetCustomerByIdAsync(int id)
        {
            var response = await _apiService.GetHttpClient().GetAsync($"odata/Customers({id})");
            response.EnsureSuccessStatusCode();

            using var responseContent = await response.Content.ReadAsStreamAsync();
            return await JsonSerializer.DeserializeAsync<Customer>(responseContent);
        }
    }
}
