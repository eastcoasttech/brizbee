using Microsoft.JSInterop;

namespace Brizbee.Dashboard.Server.Services
{
    public class LocalStorageService
    {
        private readonly IJSRuntime _js;

        public LocalStorageService(IJSRuntime js)
        {
            _js = js;
        }

        public async Task<string> GetFromLocalStorage(string key)
        {
            return await _js.InvokeAsync<string>("sessionStorage.getItem", key);
        }

        public async Task SetLocalStorage(string key, string value)
        {
            await _js.InvokeVoidAsync("sessionStorage.setItem", key, value);
        }

        public async Task RemoveFromLocalStorage(string key)
        {
            await _js.InvokeAsync<string>("sessionStorage.removeItem", key);
        }
    }
}
