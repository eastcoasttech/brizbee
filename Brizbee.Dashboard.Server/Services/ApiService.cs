namespace Brizbee.Dashboard.Server.Services;

public class ApiService(HttpClient httpClient)
{
    private readonly HttpClient httpClient = httpClient;

    public string GetBaseUrl()
    {
        if (httpClient.BaseAddress == null)
        {
            return string.Empty;
        }

        return httpClient.BaseAddress.ToString();
    }

    public HttpClient GetHttpClient()
    {
        return httpClient;
    }
}
