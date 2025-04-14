using System.Text.Json.Serialization;

namespace Brizbee.Dashboard.Server;

public class ODataListResponse<T> where T : class
{
    [JsonPropertyName("@odata.count")]
    public long? Count { get; init; }

    public IEnumerable<T> Value { get; init; }
}
