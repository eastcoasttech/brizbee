using Brizbee.Core.Models.Accounting;
using System.Text.Json;
using System.Threading.Tasks;

namespace Brizbee.Dashboard.Services
{
    public class PaycheckService
    {
        public ApiService ApiService;
        private JsonSerializerOptions _options = new()
        {
            PropertyNameCaseInsensitive = true
        };

        public PaycheckService(ApiService apiService)
        {
            ApiService = apiService;
        }
        
        public void ConfigureHeadersWithToken(string token)
        {
            // Clear old headers first
            ResetHeaders();

            ApiService.GetHttpClient().DefaultRequestHeaders.Add("Authorization", $"Bearer {token}");
        }
        
        public async Task<Paycheck> GetDraftPaycheckAsync()
        {
            var response = await ApiService.GetHttpClient().GetAsync("api/Accounting/Paychecks/Draft");
            response.EnsureSuccessStatusCode();

            await using var responseContent = await response.Content.ReadAsStreamAsync();
            return await JsonSerializer.DeserializeAsync<Paycheck>(responseContent);
        }

        public void ResetHeaders()
        {
            ApiService.GetHttpClient().DefaultRequestHeaders.Remove("Authorization");
        }
    }
}
