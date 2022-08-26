using Brizbee.Core.Models.Accounting;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text;
using System.Threading.Tasks;
using System.Net.Http;
using System.Net;

namespace Brizbee.Dashboard.Services
{
    public class InvoiceService
    {
        public ApiService _apiService;
        private JsonSerializerOptions options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };

        public InvoiceService(ApiService apiService)
        {
            _apiService = apiService;
        }
        
        public void ConfigureHeadersWithToken(string token)
        {
            // Clear old headers first
            ResetHeaders();

            _apiService.GetHttpClient().DefaultRequestHeaders.Add("Authorization", $"Bearer {token}");
        }

        public async Task<bool> DeleteInvoiceAsync(long id)
        {
            var response = await _apiService.GetHttpClient().DeleteAsync($"api/Invoices/{id}");
            if (response.IsSuccessStatusCode)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public async Task<(List<Invoice>, long?)> GetInvoicesAsync(int pageSize = 100, int skip = 0, string sortBy = "Accounts/Number", string sortDirection = "ASC", int[] objectIds = null)
        {
            var filterParameters = new StringBuilder();

            if (objectIds != null)
                filterParameters.Append(string.Join("", objectIds.Select(x => $"&objectIds={x}")));

            var response = await _apiService.GetHttpClient().GetAsync($"api/Invoices?pageSize={pageSize}&skip={skip}&orderBy={sortBy}&orderByDirection={sortDirection}{filterParameters}");

            if (!response.IsSuccessStatusCode)
                return (new List<Invoice>(0), 0);

            using var responseContent = await response.Content.ReadAsStreamAsync();
            var value = await JsonSerializer.DeserializeAsync<List<Invoice>>(responseContent, options);
            var total = long.Parse(response.Headers.GetValues("X-Paging-TotalRecordCount").FirstOrDefault());
            return (value, total);
        }

        public async Task<Invoice> GetInvoiceByIdAsync(long id)
        {
            var response = await _apiService.GetHttpClient().GetAsync($"api/Invoices/{id}");
            response.EnsureSuccessStatusCode();

            using var responseContent = await response.Content.ReadAsStreamAsync();
            return await JsonSerializer.DeserializeAsync<Invoice>(responseContent);
        }

        public async Task<Invoice> SaveInvoiceAsync(Invoice invoice)
        {
            var url = invoice.Id != 0 ? $"api/Invoices/{invoice.Id}" : "api/Invoices";
            var method = invoice.Id != 0 ? HttpMethod.Put : HttpMethod.Post;

            using (var request = new HttpRequestMessage(method, url))
            {
                var payload = new Dictionary<string, object>() {
                    { "EnteredOn", invoice.EnteredOn },
                    { "Number", invoice.Number }
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
                        if (response.IsSuccessStatusCode)
                        {
                            using var responseContent = await response.Content.ReadAsStreamAsync();

                            if (response.StatusCode == HttpStatusCode.NoContent)
                            {
                                return null;
                            }
                            else
                            {
                                var deserialized = await JsonSerializer.DeserializeAsync<Invoice>(responseContent, options);
                                return deserialized;
                            }
                        }
                        else
                        {
                            return null;
                        }
                    }
                }
            }
        }

        public void ResetHeaders()
        {
            _apiService.GetHttpClient().DefaultRequestHeaders.Remove("Authorization");
        }
    }
}
