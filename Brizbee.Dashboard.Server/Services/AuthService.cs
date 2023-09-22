using Brizbee.Core.Models;
using Brizbee.Dashboard.Server.Serialization;
using System.Text;
using System.Text.Json;

namespace Brizbee.Dashboard.Server.Services
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

        public async Task<string> AuthenticateWithPinAsync(PinSession session)
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
                            var deserialized = await JsonSerializer.DeserializeAsync<Authentication>(responseContent, options);
                            return deserialized.Token;
                        }
                        else
                        {
                            return null;
                        }
                    }
                }
            }
        }

        public async Task<string> AuthenticateWithEmailAsync(EmailSession session)
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
                            var deserialized = await JsonSerializer.DeserializeAsync<Authentication>(responseContent, options);
                            return deserialized.Token;
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
