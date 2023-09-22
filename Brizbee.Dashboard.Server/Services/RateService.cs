using Brizbee.Blazor.Server;
using Brizbee.Core.Models;
using System.Net;
using System.Text;
using System.Text.Json;

namespace Brizbee.Dashboard.Server.Services
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

        public async Task<(List<Rate>, long?)> GetBasePayrollRatesAsync(DateTime min, DateTime max)
        {
            var response = await _apiService.GetHttpClient().GetAsync($"odata/Rates/BasePayrollRatesForPunches(InAt={min.ToString("yyyy-MM-dd")},OutAt={max.ToString("yyyy-MM-dd")})?$count=true");
            response.EnsureSuccessStatusCode();

            using var responseContent = await response.Content.ReadAsStreamAsync();
            var odataResponse = await JsonSerializer.DeserializeAsync<ODataListResponse<Rate>>(responseContent, options);
            return (odataResponse.Value.ToList(), odataResponse.Count);
        }

        public async Task<(List<Rate>, long?)> GetAlternatePayrollRatesAsync(DateTime min, DateTime max)
        {
            var response = await _apiService.GetHttpClient().GetAsync($"odata/Rates/AlternatePayrollRatesForPunches(InAt={min.ToString("yyyy-MM-dd")},OutAt={max.ToString("yyyy-MM-dd")})?$count=true");
            response.EnsureSuccessStatusCode();

            using var responseContent = await response.Content.ReadAsStreamAsync();
            var odataResponse = await JsonSerializer.DeserializeAsync<ODataListResponse<Rate>>(responseContent, options);
            return (odataResponse.Value.ToList(), odataResponse.Count);
        }

        public async Task<(List<Rate>, long?)> GetBaseServiceRatesAsync(DateTime min, DateTime max)
        {
            var response = await _apiService.GetHttpClient().GetAsync($"odata/Rates/BaseServiceRatesForPunches(InAt={min.ToString("yyyy-MM-dd")},OutAt={max.ToString("yyyy-MM-dd")})?$count=true");
            response.EnsureSuccessStatusCode();

            using var responseContent = await response.Content.ReadAsStreamAsync();
            var odataResponse = await JsonSerializer.DeserializeAsync<ODataListResponse<Rate>>(responseContent, options);
            return (odataResponse.Value.ToList(), odataResponse.Count);
        }

        public async Task<(List<Rate>, long?)> GetAlternateServiceRatesAsync(DateTime min, DateTime max)
        {
            var response = await _apiService.GetHttpClient().GetAsync($"odata/Rates/AlternateServiceRatesForPunches(InAt={min.ToString("yyyy-MM-dd")},OutAt={max.ToString("yyyy-MM-dd")})?$count=true");
            response.EnsureSuccessStatusCode();

            using var responseContent = await response.Content.ReadAsStreamAsync();
            var odataResponse = await JsonSerializer.DeserializeAsync<ODataListResponse<Rate>>(responseContent, options);
            return (odataResponse.Value.ToList(), odataResponse.Count);
        }

        public async Task<(List<Rate>, long?)> GetRatesAsync(int pageSize = 100, int skip = 0, string sortBy = "Name", string sortDirection = "ASC")
        {
            var response = await _apiService.GetHttpClient().GetAsync($"odata/Rates?$count=true&$expand=ParentRate&$top={pageSize}&$skip={skip}&$orderby={sortBy} {sortDirection}");
            response.EnsureSuccessStatusCode();

            using var responseContent = await response.Content.ReadAsStreamAsync();
            var odataResponse = await JsonSerializer.DeserializeAsync<ODataListResponse<Rate>>(responseContent, options);
            return (odataResponse.Value.ToList(), odataResponse.Count);
        }
        
        public async Task<(List<Rate>, long?)> GetBaseRatesAsync(string scope = "Payroll")
        {
            var filter = "";
            if (scope == "Payroll")
            {
                filter = "and Type eq 'Payroll'";
            }
            else
            {
                filter = "and Type eq 'Service'";
            }

            var response = await _apiService.GetHttpClient().GetAsync($"odata/Rates?$count=true&$filter=ParentRateId eq null {filter}&$orderby=Name");
            response.EnsureSuccessStatusCode();

            using var responseContent = await response.Content.ReadAsStreamAsync();
            var odataResponse = await JsonSerializer.DeserializeAsync<ODataListResponse<Rate>>(responseContent, options);
            return (odataResponse.Value.ToList(), odataResponse.Count);
        }

        public async Task<Rate> GetRateByIdAsync(int id)
        {
            var response = await _apiService.GetHttpClient().GetAsync($"odata/Rates({id})");
            response.EnsureSuccessStatusCode();

            using var responseContent = await response.Content.ReadAsStreamAsync();
            return await JsonSerializer.DeserializeAsync<Rate>(responseContent, options);
        }

        public async Task<bool> DeleteRateAsync(int id)
        {
            var response = await _apiService.GetHttpClient().DeleteAsync($"odata/Rates({id})");
            if (response.IsSuccessStatusCode)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public async Task<Rate> SaveRateAsync(Rate rate, string scope = "Base")
        {
            var url = rate.Id != 0 ? $"odata/Rates({rate.Id})" : "odata/Rates";
            var method = rate.Id != 0 ? HttpMethod.Patch : HttpMethod.Post;

            using (var request = new HttpRequestMessage(method, url))
            {
                var payload = new Dictionary<string, object>() {
                    { "Name", rate.Name },
                    { "QBDPayrollItem", rate.QBDPayrollItem },
                    { "QBDServiceItem", rate.QBDServiceItem }
                };

                // Some properties can only be added at creation.
                if (rate.Id == 0)
                {
                    payload.Add("Type", rate.Type);

                    if (scope == "Alternate")
                    {
                        payload.Add("ParentRateId", rate.ParentRateId);
                    }
                }

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
                                var deserialized = await JsonSerializer.DeserializeAsync<Rate>(responseContent, options);
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
    }
}
