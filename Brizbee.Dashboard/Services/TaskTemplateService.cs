using Brizbee.Blazor;
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
    public class TaskTemplateService
    {
        public ApiService _apiService;
        private JsonSerializerOptions options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };

        public TaskTemplateService(ApiService apiService)
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

        public async Task<(List<TaskTemplate>, long?)> GetTaskTemplatesAsync()
        {
            var response = await _apiService.GetHttpClient().GetAsync($"odata/TaskTemplates?$count=true");

            if (response.IsSuccessStatusCode)
            {
                using var responseContent = await response.Content.ReadAsStreamAsync();
                var odataResponse = await JsonSerializer.DeserializeAsync<ODataListResponse<TaskTemplate>>(responseContent, options);
                return (odataResponse.Value.ToList(), odataResponse.Count);
            }
            else
            {
                return (new List<TaskTemplate>(), 0);
            }
        }

        public async Task<TaskTemplate> GetTaskTemplateByIdAsync(int id)
        {
            var response = await _apiService.GetHttpClient().GetAsync($"odata/TaskTemplates({id})");

            if (response.IsSuccessStatusCode)
            {
                using var responseContent = await response.Content.ReadAsStreamAsync();
                return await JsonSerializer.DeserializeAsync<TaskTemplate>(responseContent);
            }
            else
            {
                return null;
            }
        }

        public async Task<bool> DeleteTaskTemplateAsync(int id)
        {
            var response = await _apiService.GetHttpClient().DeleteAsync($"odata/TaskTemplates({id})");

            if (response.IsSuccessStatusCode)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public async Task<TaskTemplate> SaveTaskTemplateAsync(TaskTemplate template)
        {
            using (var request = new HttpRequestMessage(HttpMethod.Post, "odata/TaskTemplates"))
            {
                var payload = new Dictionary<string, object>() {
                    { "Name", template.Name },
                    { "Template", template.Template }
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
                                var deserialized = await JsonSerializer.DeserializeAsync<TaskTemplate>(responseContent, options);
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
