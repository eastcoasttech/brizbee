using Brizbee.Core.Models.Accounting;
using System.Text.Json;

namespace Brizbee.Dashboard.Server.Services;

public class EntryService
{
    public ApiService ApiService;
    private readonly JsonSerializerOptions _options = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public EntryService(ApiService apiService)
    {
        ApiService = apiService;
    }
        
    public void ConfigureHeadersWithToken(string token)
    {
        // Clear old headers first
        ResetHeaders();

        ApiService.GetHttpClient().DefaultRequestHeaders.Add("Authorization", $"Bearer {token}");
    }

    public void ResetHeaders()
    {
        ApiService.GetHttpClient().DefaultRequestHeaders.Remove("Authorization");
    }

    public async Task<(bool Success, string Message, List<Entry>? Results, long? TotalRecordCount)> GetEntriesAsync(long accountId, int pageSize = 100, int skip = 0, string sortBy = "TRANSACTIONS/ENTERED_ON", string sortDirection = "ASC")
    {
        try
        {
            var response = await ApiService.GetHttpClient().GetAsync($"api/Accounting/Entries?pageSize={pageSize}&skip={skip}&orderBy={sortBy}&orderByDirection={sortDirection}&filterAccountId={accountId}");

            response.EnsureSuccessStatusCode();

            await using var responseContent = await response.Content.ReadAsStreamAsync();
            var value = await JsonSerializer.DeserializeAsync<List<Entry>>(responseContent, _options);
            var totalRecordCount = response.Headers.GetValues("X-Paging-TotalRecordCount").FirstOrDefault();

            if (string.IsNullOrEmpty(totalRecordCount))
            {
                return (false, "Total Record Count is empty.", null, null);
            }

            var total = long.Parse(totalRecordCount);

            return (true, string.Empty, value, total);
        }
        catch (JsonException ex)
        {
            return (false, ex.Message, null, null);
        }
        catch (ArgumentNullException ex)
        {
            return (false, ex.Message, null, null);
        }
        catch (NotSupportedException ex)
        {
            return (false, ex.Message, null, null);
        }
        catch (InvalidOperationException ex)
        {
            return (false, ex.Message, null, null);
        }
        catch (HttpRequestException ex)
        {
            return (false, ex.Message, null, null);
        }
        catch (TaskCanceledException ex)
        {
            return (false, ex.Message, null, null);
        }
        catch (UriFormatException ex)
        {
            return (false, ex.Message, null, null);
        }
    }
}
