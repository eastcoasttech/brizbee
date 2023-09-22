using Brizbee.Core.Models;
using System.Text.Json;

namespace Brizbee.Dashboard.Server.Services
{
    public class LockService
    {
        public ApiService _apiService;
        private JsonSerializerOptions options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };

        public LockService(ApiService apiService)
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

        public async Task<(List<Commit>, long?)> GetLocksAsync(int pageSize = 20, int skip = 0, string sortBy = "LOCK/INAT", string sortDirection = "ASC")
        {
            var response = await _apiService.GetHttpClient().GetAsync($"api/Locks?pageSize={pageSize}&skip={skip}&orderBy={sortBy}&orderByDirection={sortDirection}");

            if (!response.IsSuccessStatusCode)
                return (new List<Commit>(0), 0);

            using var responseContent = await response.Content.ReadAsStreamAsync();
            var value = await JsonSerializer.DeserializeAsync<List<Commit>>(responseContent, options);
            var total = long.Parse(response.Headers.GetValues("X-Paging-TotalRecordCount").FirstOrDefault());
            return (value, total);
        }

        public async Task<bool> PostUndoAsync(int commitId)
        {
            using (var request = new HttpRequestMessage(HttpMethod.Post, $"api/Locks/{commitId}/Undo"))
            {
                using (var response = await _apiService
                    .GetHttpClient()
                    .SendAsync(request, HttpCompletionOption.ResponseHeadersRead)
                    .ConfigureAwait(false))
                {
                    if (response.IsSuccessStatusCode)
                    {
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                }
            }
        }
    }
}
