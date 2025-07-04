﻿using System.Net;
using System.Text;
using System.Text.Json;
using Brizbee.Core.Models;

namespace Brizbee.Dashboard.Server.Services
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
            return await JsonSerializer.DeserializeAsync<QBDInventoryItem>(responseContent, options);
        }

        public async Task<bool> DeleteQbdInventoryItemAsync(long id)
        {
            var response = await _apiService.GetHttpClient().DeleteAsync($"api/QBDInventoryItems/{id}");
            return response.IsSuccessStatusCode;
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
