using Brizbee.Core.Models;
using System.Net;
using System.Text;
using System.Text.Json;

namespace Brizbee.Dashboard.Server.Services
{
    public class PopulateTemplateService
    {
        public ApiService _apiService;
        private JsonSerializerOptions options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };

        public PopulateTemplateService(ApiService apiService)
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

        public async Task<(List<PopulateTemplate>, long?)> GetPopulateTemplatesAsync(int pageSize = 100, int skip = 0, string sortBy = "Number", string sortDirection = "ASC")
        {
            var response = await _apiService.GetHttpClient().GetAsync($"api/PopulateTemplates?pageSize={pageSize}&skip={skip}&orderBy={sortBy}&orderByDirection={sortDirection}");

            if (!response.IsSuccessStatusCode)
                return (new List<PopulateTemplate>(0), 0);

            using var responseContent = await response.Content.ReadAsStreamAsync();
            var value = await JsonSerializer.DeserializeAsync<List<PopulateTemplate>>(responseContent, options);
            var total = long.Parse(response.Headers.GetValues("X-Paging-TotalRecordCount").FirstOrDefault());
            return (value, total);
        }

        public async Task<PopulateTemplate> SavePopulateTemplateAsync(PopulateTemplate populateTemplate)
        {
            using (var request = new HttpRequestMessage(HttpMethod.Post, "api/PopulateTemplates"))
            {
                var payload = new Dictionary<string, object>() {
                    { "Name", populateTemplate.Name },
                    { "Template", populateTemplate.Template },
                    { "RateType", populateTemplate.RateType }
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
                                var deserialized = await JsonSerializer.DeserializeAsync<PopulateTemplate>(responseContent, options);
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
