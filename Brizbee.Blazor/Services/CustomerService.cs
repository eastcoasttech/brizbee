using Brizbee.Common.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace Brizbee.Blazor.Services
{
    public class CustomerService
    {
        public HttpClient _httpClient;

        public CustomerService(HttpClient client)
        {
            _httpClient = client;
        }

        public async Task<List<Customer>> GetContactsAsync()
        {
            var response = await _httpClient.GetAsync("odata/Customers");
            response.EnsureSuccessStatusCode();

            //Trace.TraceInformation(await response.Content.ReadAsStringAsync());

            using var responseContent = await response.Content.ReadAsStreamAsync();
            var odataResponse = await JsonSerializer.DeserializeAsync<ODataResponse<Customer>>(responseContent);
            return odataResponse.Value.ToList();
        }

        public async Task<Customer> GetContactByIdAsync(int id)
        {
            var response = await _httpClient.GetAsync($"odata/Customers({id})");
            response.EnsureSuccessStatusCode();

            using var responseContent = await response.Content.ReadAsStreamAsync();
            return await JsonSerializer.DeserializeAsync<Customer>(responseContent);
        }
    }
}
