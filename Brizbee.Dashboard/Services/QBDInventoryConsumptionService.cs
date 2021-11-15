using Brizbee.Common.Models;
using Brizbee.Common.Security;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace Brizbee.Dashboard.Services
{
    public class QBDInventoryConsumptionService
    {
        public ApiService _apiService;
        private JsonSerializerOptions options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };

        public QBDInventoryConsumptionService(ApiService apiService)
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

        public async Task<(List<QBDInventoryConsumption>, long?)> GetQBDInventoryConsumptionsAsync(int pageSize = 100, int skip = 0, string sortBy = "QBDINVENTORYCONSUMPTIONS/CREATEDAT", string sortDirection = "ASC")
        {
            var response = await _apiService.GetHttpClient().GetAsync($"api/QBDInventoryConsumptions?pageSize={pageSize}&skip={skip}&orderBy={sortBy}&orderByDirection={sortDirection}");
            response.EnsureSuccessStatusCode();

            using var responseContent = await response.Content.ReadAsStreamAsync();
            var value = await JsonSerializer.DeserializeAsync<List<QBDInventoryConsumption>>(responseContent, options);
            var total = long.Parse(response.Headers.GetValues("X-Paging-TotalRecordCount").FirstOrDefault());
            return (value, total);
        }

        public async Task<QBDInventoryConsumption> GetQBDInventoryConsumptionByIdAsync(long id)
        {
            var response = await _apiService.GetHttpClient().GetAsync($"api/QBDInventoryConsumptions/{id}");
            response.EnsureSuccessStatusCode();

            using var responseContent = await response.Content.ReadAsStreamAsync();
            return await JsonSerializer.DeserializeAsync<QBDInventoryConsumption>(responseContent);
        }

        public async Task<bool> DeleteQBDInventoryConsumptionAsync(long id)
        {
            var response = await _apiService.GetHttpClient().DeleteAsync($"api/QBDInventoryConsumptions/{id}");
            if (response.IsSuccessStatusCode)
            {
                return true;
            }
            else
            {
                return false;
            }
        }
    }
}
