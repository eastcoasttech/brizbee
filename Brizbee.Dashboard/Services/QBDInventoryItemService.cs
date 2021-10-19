using Brizbee.Common.Models;
using Brizbee.Common.Security;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Brizbee.Dashboard.Services
{
    public class QBDInventoryItemService
    {
        public ApiService _apiService;
        private JsonSerializerOptions options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };

        public QBDInventoryItemService(ApiService apiService)
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

        public async Task<(List<QBDInventoryItem>, long?)> GetQBDInventoryItemsAsync(int pageSize = 100, int skip = 0, string sortBy = "QBDInventoryItems/Name", string sortDirection = "ASC")
        {
            var response = await _apiService.GetHttpClient().GetAsync($"api/QBDInventoryItems?pageSize={pageSize}&skip={skip}&orderBy={sortBy}&orderByDirection={sortDirection}");
            response.EnsureSuccessStatusCode();

            using var responseContent = await response.Content.ReadAsStreamAsync();
            var value = await JsonSerializer.DeserializeAsync<List<QBDInventoryItem>>(responseContent, options);
            var total = long.Parse(response.Headers.GetValues("X-Paging-TotalRecordCount").FirstOrDefault());
            return (value, total);
        }

        public async Task<QBDInventoryItem> GetQBDInventoryItemByIdAsync(long id)
        {
            var response = await _apiService.GetHttpClient().GetAsync($"api/QBDInventoryItems/{id}");
            response.EnsureSuccessStatusCode();

            using var responseContent = await response.Content.ReadAsStreamAsync();
            return await JsonSerializer.DeserializeAsync<QBDInventoryItem>(responseContent);
        }

        public async Task<(bool, string)> SaveQBDInventoryItemAsync(QBDInventoryItem inventoryItem)
        {
            using (var request = new HttpRequestMessage(HttpMethod.Put, $"api/QBDInventoryItems/{inventoryItem.Id}"))
            {
                var payload = new Dictionary<string, object>() {
                    { "CustomBarCodeValue", inventoryItem.CustomBarCodeValue }
                };

                var json = JsonSerializer.Serialize(payload, options);

                using (var stringContent = new StringContent(json, Encoding.UTF8, "application/json"))
                {
                    request.Content = stringContent;

                    using (var response = await _apiService
                        .GetHttpClient()
                        .SendAsync(request, HttpCompletionOption.ResponseHeadersRead)
                        .ConfigureAwait(false))
                    {
                        using var responseContent = await response.Content.ReadAsStreamAsync();

                        if (response.IsSuccessStatusCode)
                        {
                            if (response.StatusCode == HttpStatusCode.OK)
                                return (true, "");
                            else
                                return (false, responseContent.ToString());
                        }
                        else
                        {
                            return (false, responseContent.ToString());
                        }
                    }
                }
            }
        }
    }
}
