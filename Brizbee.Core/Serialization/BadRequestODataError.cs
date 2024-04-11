using System.Text.Json.Serialization;

namespace Brizbee.Core.Serialization;

public class BadRequestODataError
{
    [JsonPropertyName("error")]
    public BadRequestODataMessage? Error {get;set; }
}
