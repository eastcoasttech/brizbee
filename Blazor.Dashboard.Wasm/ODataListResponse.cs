using System.Text.Json.Serialization;

namespace Brizbee.Blazor.Dashboard.Wasm
{
    public class ODataListResponse<T> where T : class
    {
        [JsonPropertyName("@odata.count")]
        public long? Count { get; set; }

        public IEnumerable<T> Value { get; set; }
    }
}
