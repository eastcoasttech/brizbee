﻿using Brizbee.Dashboard.Models;
using Brizbee.Dashboard.Security;
using Brizbee.Dashboard.Serialization.Alerts;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Brizbee.Dashboard.Services
{
    public class OrganizationService
    {
        public ApiService _apiService;
        private JsonSerializerOptions options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };

        public OrganizationService(ApiService apiService)
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

        public async Task<Organization> GetOrganizationByIdAsync(int id)
        {
            var response = await _apiService.GetHttpClient().GetAsync($"odata/Organizations({id})");
            response.EnsureSuccessStatusCode();

            using var responseContent = await response.Content.ReadAsStreamAsync();
            return await JsonSerializer.DeserializeAsync<Organization>(responseContent, options);
        }

        public async Task<bool> SaveOrganizationAsync(Organization organization)
        {
            using (var request = new HttpRequestMessage(HttpMethod.Patch, $"odata/Organizations({organization.Id})"))
            {
                var payload = new Dictionary<string, object>() {
                    { "Name", organization.Name },
                    { "MinutesFormat", organization.MinutesFormat },
                    { "Code", organization.Code },
                    { "Groups", organization.Groups },
                    { "ShowCustomerNumber", organization.ShowCustomerNumber },
                    { "ShowProjectNumber", organization.ShowProjectNumber },
                    { "ShowTaskNumber", organization.ShowTaskNumber },
                    { "SortCustomersByColumn", organization.SortCustomersByColumn },
                    { "SortProjectsByColumn", organization.SortProjectsByColumn },
                    { "SortTasksByColumn", organization.SortTasksByColumn }
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

        public async Task<Organization> SaveSourceIdAsync(int organizationId, string sourceId)
        {
            using (var request = new HttpRequestMessage(HttpMethod.Patch, $"odata/Organizations({organizationId})"))
            {
                var payload = new Dictionary<string, object>() {
                    { "StripeSourceId", sourceId }
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
                            return null;
                        }
                        else
                        {
                            return null;
                        }
                    }
                }
            }
        }

        public async Task<List<Alert>> GetAlerts(int organizationId)
        {
            var response = await _apiService.GetHttpClient().GetAsync($"api/OrganizationsExpanded/{organizationId}/Alerts");

            if (!response.IsSuccessStatusCode)
                return new List<Alert>();

            using var responseContent = await response.Content.ReadAsStreamAsync();
            return await JsonSerializer.DeserializeAsync<List<Alert>>(responseContent, options);
        }
    }
}
