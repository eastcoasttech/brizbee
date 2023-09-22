using Brizbee.Blazor.Server;
using Brizbee.Core.Models;
using Brizbee.Core.Serialization.Statistics;
using System.Net;
using System.Text;
using System.Text.Json;

namespace Brizbee.Dashboard.Server.Services
{
    public class JobService
    {
        public ApiService _apiService;
        private JsonSerializerOptions options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };

        public JobService(ApiService apiService)
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

        public async Task<(List<Job>, long?)> GetExpandedJobsAsync(int pageSize = 100, int skip = 0, string sortBy = "Number", string sortDirection = "ASC")
        {
            var response = await _apiService.GetHttpClient().GetAsync($"api/JobsExpanded?pageSize={pageSize}&skip={skip}&orderBy={sortBy}&orderByDirection={sortDirection}");

            if (!response.IsSuccessStatusCode)
                return (new List<Job>(0), 0);

            using var responseContent = await response.Content.ReadAsStreamAsync();
            var value = await JsonSerializer.DeserializeAsync<List<Job>>(responseContent, options);
            var total = long.Parse(response.Headers.GetValues("X-Paging-TotalRecordCount").FirstOrDefault());
            return (value, total);
        }

        public async Task<(List<Job>, long?)> GetJobsAsync(int customerId, int pageSize = 100, int skip = 0, string sortBy = "Number", string sortDirection = "ASC")
        {
            var response = await _apiService.GetHttpClient().GetAsync($"odata/Jobs?$count=true&$top={pageSize}&$skip={skip}&$orderby={sortBy} {sortDirection}&$filter=CustomerId eq {customerId}");

            if (!response.IsSuccessStatusCode)
                return (new List<Job>(0), 0);

            using var responseContent = await response.Content.ReadAsStreamAsync();
            var odataResponse = await JsonSerializer.DeserializeAsync<ODataListResponse<Job>>(responseContent, options);
            return (odataResponse.Value.ToList(), odataResponse.Count);
        }

        public async Task<(List<Job>, long?)> GetProposedJobsAsync(int pageSize = 100, int skip = 0, string sortBy = "Number", string sortDirection = "ASC")
        {
            var response = await _apiService.GetHttpClient().GetAsync($"odata/Jobs/Proposed()?$expand=Customer&$count=true&$top={pageSize}&$skip={skip}&$orderby={sortBy} {sortDirection}");
            
            if (!response.IsSuccessStatusCode)
                return (new List<Job>(), 0);

            using var responseContent = await response.Content.ReadAsStreamAsync();
            var odataResponse = await JsonSerializer.DeserializeAsync<ODataListResponse<Job>>(responseContent, options);
            return (odataResponse.Value.ToList(), odataResponse.Count);
        }

        public async Task<(List<Job>, long?)> GetOpenJobsAsync(int pageSize = 100, int skip = 0, string sortBy = "Number", string sortDirection = "ASC")
        {
            var response = await _apiService.GetHttpClient().GetAsync($"odata/Jobs/Open()?$expand=Customer&$count=true&$top={pageSize}&$skip={skip}&$orderby={sortBy} {sortDirection}");
            
            if (!response.IsSuccessStatusCode)
                return (new List<Job>(), 0);

            using var responseContent = await response.Content.ReadAsStreamAsync();
            var odataResponse = await JsonSerializer.DeserializeAsync<ODataListResponse<Job>>(responseContent, options);
            return (odataResponse.Value.ToList(), odataResponse.Count);
        }

        public async Task<(List<Job>, long?)> GetClosedJobsAsync(int pageSize = 100, int skip = 0, string sortBy = "Number", string sortDirection = "ASC")
        {
            var response = await _apiService.GetHttpClient().GetAsync($"odata/Jobs/Closed()?$expand=Customer&$count=true&$top={pageSize}&$skip={skip}&$orderby={sortBy} {sortDirection}");
            
            if (!response.IsSuccessStatusCode)
                return (new List<Job>(), 0);

            using var responseContent = await response.Content.ReadAsStreamAsync();
            var odataResponse = await JsonSerializer.DeserializeAsync<ODataListResponse<Job>>(responseContent, options);
            return (odataResponse.Value.ToList(), odataResponse.Count);
        }
        
        public async Task<Job> GetJobByIdAsync(int id)
        {
            var response = await _apiService.GetHttpClient().GetAsync($"odata/Jobs({id})?$expand=Customer");
            response.EnsureSuccessStatusCode();

            using var responseContent = await response.Content.ReadAsStreamAsync();
            return await JsonSerializer.DeserializeAsync<Job>(responseContent, options);
        }

        public async Task<ProjectStatistics> GetStatisticsAsync(int id)
        {
            var response = await _apiService.GetHttpClient().GetAsync($"api/JobsExpanded/{id}/Statistics");
            response.EnsureSuccessStatusCode();

            using var responseContent = await response.Content.ReadAsStreamAsync();
            return await JsonSerializer.DeserializeAsync<ProjectStatistics>(responseContent, options);
        }

        public async Task<List<Job>> SearchJobsAsync(string query)
        {
            var response = await _apiService.GetHttpClient().GetAsync($"odata/Jobs?$filter=contains(Name,'{query}')&$select=Name,Number,Id&$expand=Customer($select=Name,Number)");
            response.EnsureSuccessStatusCode();

            using var responseContent = await response.Content.ReadAsStreamAsync();
            var odataResponse = await JsonSerializer.DeserializeAsync<ODataListResponse<Job>>(responseContent, options);
            return odataResponse.Value.ToList();
        }

        public async Task<bool> DeleteJobAsync(int id)
        {
            var response = await _apiService.GetHttpClient().DeleteAsync($"odata/Jobs({id})");
            if (response.IsSuccessStatusCode)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public async Task<Job> SaveJobAsync(Job job)
        {
            var url = job.Id != 0 ? $"odata/Jobs({job.Id})" : "odata/Jobs";
            var method = job.Id != 0 ? HttpMethod.Patch : HttpMethod.Post;

            using (var request = new HttpRequestMessage(method, url))
            {
                var payload = new Dictionary<string, object>() {
                    { "Name", job.Name },
                    { "Number", job.Number },
                    { "Description", job.Description },
                    { "Status", job.Status },
                    { "CustomerWorkOrder", job.CustomerWorkOrder },
                    { "CustomerPurchaseOrder", job.CustomerPurchaseOrder },
                    { "InvoiceNumber", job.InvoiceNumber },
                    { "QuoteNumber", job.QuoteNumber },
                    { "QuickBooksCustomerJob", job.QuickBooksCustomerJob },
                    { "QuickBooksClass", job.QuickBooksClass },
                    { "Taxability", job.Taxability }
                };

                // Can only be configured at creation.
                if (job.Id == 0)
                {
                    payload.Add("CustomerId", job.CustomerId);

                    // Optional, task template.
                    if (job.TaskTemplateId.HasValue)
                        payload.Add("TaskTemplateId", job.TaskTemplateId);
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
                                var deserialized = await JsonSerializer.DeserializeAsync<Job>(responseContent, options);
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

        public async Task<string> GetNextNumberAsync()
        {
            var response = await _apiService.GetHttpClient().PostAsync("odata/Jobs/NextNumber", new StringContent(""));
            response.EnsureSuccessStatusCode();

            using var responseContent = await response.Content.ReadAsStreamAsync();
            var odataResponse = await JsonSerializer.DeserializeAsync<ODataSingleResponse<string>>(responseContent, options);
            return odataResponse.Value;
        }
    }
}
