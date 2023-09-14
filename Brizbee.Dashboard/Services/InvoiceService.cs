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
        public ApiService ApiService;
        private readonly JsonSerializerOptions _options = new()
        {
            PropertyNameCaseInsensitive = true
        };

        public InvoiceService(ApiService apiService)
        {
            ApiService = apiService;
        }
        
        public void ConfigureHeadersWithToken(string token)
        {
            // Clear old headers first
            ResetHeaders();

            ApiService.GetHttpClient().DefaultRequestHeaders.Add("Authorization", $"Bearer {token}");
        }

        public async Task<bool> DeleteInvoiceAsync(long id)
        {
            var response = await ApiService.GetHttpClient().DeleteAsync($"api/Accounting/Invoices/{id}");
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

            var response = await ApiService.GetHttpClient().GetAsync($"api/Accounting/Invoices?pageSize={pageSize}&skip={skip}&orderBy={sortBy}&orderByDirection={sortDirection}{filterParameters}");

            if (!response.IsSuccessStatusCode)
                return (new List<Invoice>(0), 0);

            await using var responseContent = await response.Content.ReadAsStreamAsync();
            var value = await JsonSerializer.DeserializeAsync<List<Invoice>>(responseContent, _options);
            var total = long.Parse(response.Headers.GetValues("X-Paging-TotalRecordCount").FirstOrDefault());
            return (value, total);
        }

        public async Task<Invoice> GetInvoiceByIdAsync(long id)
        {
            var response = await ApiService.GetHttpClient().GetAsync($"api/Accounting/Invoices/{id}");
            response.EnsureSuccessStatusCode();

            await using var responseContent = await response.Content.ReadAsStreamAsync();
            return await JsonSerializer.DeserializeAsync<Invoice>(responseContent);
        }

        public async Task<Invoice> SaveInvoiceAsync(Invoice invoice)
        {
            var url = invoice.Id != 0 ? $"api/Accounting/Invoices/{invoice.Id}" : "api/Accounting/Invoices";
            var method = invoice.Id != 0 ? HttpMethod.Put : HttpMethod.Post;

            using var request = new HttpRequestMessage(method, url);
            var payload = new Dictionary<string, object>() {
                { "EnteredOn", invoice.EnteredOn },
                { "Number", invoice.Number }
            };

            var json = JsonSerializer.Serialize(payload, _options);

            using var stringContent = new StringContent(json, Encoding.UTF8, "application/json");
            request.Content = stringContent;

            using var response = await ApiService
                .GetHttpClient()
                .SendAsync(request, HttpCompletionOption.ResponseHeadersRead)
                .ConfigureAwait(false);
            if (response.IsSuccessStatusCode)
            {
                await using var responseContent = await response.Content.ReadAsStreamAsync();

                if (response.StatusCode == HttpStatusCode.NoContent)
                {
                    return null;
                }
                else
                {
                    var deserialized = await JsonSerializer.DeserializeAsync<Invoice>(responseContent, _options);
                    return deserialized;
                }
            }
            else
            {
                return null;
            }
        }

        public void ResetHeaders()
        {
            ApiService.GetHttpClient().DefaultRequestHeaders.Remove("Authorization");
        }
    }
}
