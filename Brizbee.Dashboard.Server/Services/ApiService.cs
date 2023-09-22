namespace Brizbee.Dashboard.Server.Services
{
    public class ApiService
    {
        private readonly HttpClient httpClient;

        public ApiService(HttpClient httpClient)
        {
            this.httpClient = httpClient;
        }

        public string GetBaseUrl()
        {
            return httpClient.BaseAddress.ToString();
        }

        public HttpClient GetHttpClient()
        {
            return httpClient;
        }
    }
}
