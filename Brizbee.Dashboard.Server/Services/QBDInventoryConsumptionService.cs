using Brizbee.Core.Models;
using System.Net;
using System.Text;
using System.Text.Json;

namespace Brizbee.Dashboard.Server.Services
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
            return await JsonSerializer.DeserializeAsync<QBDInventoryConsumption>(responseContent, options);
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

        public async Task<(bool, string)> SaveQbdInventoryConsumptionAsync(QBDInventoryConsumption inventoryConsumption)
        {
            using var request = new HttpRequestMessage(HttpMethod.Put, $"api/QBDInventoryConsumptions/{inventoryConsumption.Id}");

            var payload = new Dictionary<string, object>() {
                { "Quantity", inventoryConsumption.Quantity }
            };

            var json = JsonSerializer.Serialize(payload, options);

            using var stringContent = new StringContent(json, Encoding.UTF8, "application/json");
            request.Content = stringContent;

            using var response = await _apiService
                .GetHttpClient()
                .SendAsync(request, HttpCompletionOption.ResponseHeadersRead)
                .ConfigureAwait(false);
            await using var responseContent = await response.Content.ReadAsStreamAsync();

            if (response.IsSuccessStatusCode)
            {
                if (response.StatusCode == HttpStatusCode.OK)
                {
                    return (true, "");
                }

                return (false, responseContent.ToString());
            }
            
            return (false, responseContent.ToString());
        }
    }
}
