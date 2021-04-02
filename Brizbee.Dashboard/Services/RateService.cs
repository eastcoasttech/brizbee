using Brizbee.Blazor;
using Brizbee.Common.Models;
using Brizbee.Common.Security;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace Brizbee.Dashboard.Services
{
    public class RateService
    {
        public ApiService _apiService;
        private JsonSerializerOptions options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };

        public RateService(ApiService apiService)
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

        public async Task<(List<Rate>, long?)> GetBasePayrollRatesAsync(DateTime min, DateTime max)
        {
            var response = await _apiService.GetHttpClient().GetAsync($"odata/Rates/Default.BasePayrollRatesForPunches(InAt='{min.ToString("yyyy-MM-dd")}',OutAt='{max.ToString("yyyy-MM-dd")}')?$count=true");
            response.EnsureSuccessStatusCode();

            using var responseContent = await response.Content.ReadAsStreamAsync();
            var odataResponse = await JsonSerializer.DeserializeAsync<ODataResponse<Rate>>(responseContent, options);
            return (odataResponse.Value.ToList(), odataResponse.Count);
        }

        public async Task<(List<Rate>, long?)> GetAlternatePayrollRatesAsync(DateTime min, DateTime max)
        {
            var response = await _apiService.GetHttpClient().GetAsync($"odata/Rates/Default.AlternatePayrollRatesForPunches(InAt='{min.ToString("yyyy-MM-dd")}',OutAt='{max.ToString("yyyy-MM-dd")}')?$count=true");
            response.EnsureSuccessStatusCode();

            using var responseContent = await response.Content.ReadAsStreamAsync();
            var odataResponse = await JsonSerializer.DeserializeAsync<ODataResponse<Rate>>(responseContent, options);
            return (odataResponse.Value.ToList(), odataResponse.Count);
        }

        public async Task<(List<Rate>, long?)> GetBaseServiceRatesAsync(DateTime min, DateTime max)
        {
            var response = await _apiService.GetHttpClient().GetAsync($"odata/Rates/Default.BaseServiceRatesForPunches(InAt='{min.ToString("yyyy-MM-dd")}',OutAt='{max.ToString("yyyy-MM-dd")}')?$count=true");
            response.EnsureSuccessStatusCode();

            using var responseContent = await response.Content.ReadAsStreamAsync();
            var odataResponse = await JsonSerializer.DeserializeAsync<ODataResponse<Rate>>(responseContent, options);
            return (odataResponse.Value.ToList(), odataResponse.Count);
        }

        public async Task<(List<Rate>, long?)> GetAlternateServiceRatesAsync(DateTime min, DateTime max)
        {
            var response = await _apiService.GetHttpClient().GetAsync($"odata/Rates/Default.AlternateServiceRatesForPunches(InAt='{min.ToString("yyyy-MM-dd")}',OutAt='{max.ToString("yyyy-MM-dd")}')?$count=true");
            response.EnsureSuccessStatusCode();

            using var responseContent = await response.Content.ReadAsStreamAsync();
            var odataResponse = await JsonSerializer.DeserializeAsync<ODataResponse<Rate>>(responseContent, options);
            return (odataResponse.Value.ToList(), odataResponse.Count);
        }

        public async Task<(List<Rate>, long?)> GetRatesAsync(int pageSize = 20, int skip = 0, string sortBy = "Name", string sortDirection = "ASC")
        {
            var response = await _apiService.GetHttpClient().GetAsync($"odata/Rates?$count=true&$expand=ParentRate&$top={pageSize}&$skip={skip}&$orderby={sortBy} {sortDirection}");
            response.EnsureSuccessStatusCode();

            using var responseContent = await response.Content.ReadAsStreamAsync();
            var odataResponse = await JsonSerializer.DeserializeAsync<ODataResponse<Rate>>(responseContent, options);
            return (odataResponse.Value.ToList(), odataResponse.Count);
        }
    }
}
