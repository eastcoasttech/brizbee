using System.Text.Json.Serialization;

namespace Brizbee.Core.Serialization;

public class BadRequestODataMessage
{
    [JsonPropertyName("code")]
    public string Code { get; set; } = string.Empty;

    [JsonPropertyName("message")]
    public string Message { get; set; } = string.Empty;
}
