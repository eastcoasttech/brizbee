using Brizbee.Dashboard.Models;
using Brizbee.Dashboard.Security;
using Brizbee.Dashboard.Serialization;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Brizbee.Dashboard.Services
{
    public class AuthService
    {
        public ApiService _apiService;
        private JsonSerializerOptions options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };

        public AuthService(ApiService apiService)
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

        public async Task<Credential> AuthenticateWithPinAsync(PinSession session)
        {
            using (var request = new HttpRequestMessage(HttpMethod.Post, "api/Auth/Authenticate"))
            {
                var payload = new
                {
                    PinOrganizationCode = session.OrganizationCode,
                    Method = "pin",
                    PinUserPin = session.UserPin
                };

                var json = JsonSerializer.Serialize(payload);
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
                            var deserialized = await JsonSerializer.DeserializeAsync<Credential>(responseContent, options);
                            return deserialized;
                        }
                        else
                        {
                            return null;
                        }
                    }
                }
            }
        }

        public async Task<Credential> AuthenticateWithEmailAsync(EmailSession session)
        {
            using (var request = new HttpRequestMessage(HttpMethod.Post, "api/Auth/Authenticate"))
            {
                var payload = new
                {
                    EmailAddress = session.EmailAddress,
                    Method = "email",
                    EmailPassword = session.EmailPassword
                };

                var json = JsonSerializer.Serialize(payload);
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
                            var deserialized = await JsonSerializer.DeserializeAsync<Credential>(responseContent, options);
                            return deserialized;
                        }
                        else
                        {
                            return null;
                        }
                    }
                }
            }
        }

        public async Task<User> GetMeAsync()
        {
            var response = await _apiService
                .GetHttpClient()
                .GetAsync($"api/Auth/Me");

            if (response.IsSuccessStatusCode)
            {
                using var responseContent = await response.Content.ReadAsStreamAsync();
                var deserialized = await JsonSerializer.DeserializeAsync<User>(responseContent, options);
                return deserialized;
            }
            else
            {
                return null;
            }
        }
    }
}
