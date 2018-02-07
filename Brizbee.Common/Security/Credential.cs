using Newtonsoft.Json;
using System.ComponentModel.DataAnnotations.Schema;

namespace Brizbee.Common.Security
{
    public class Credential
    {
        public string AuthUserId { get; set; }
        public string AuthExpiration { get; set; }
        public string AuthToken { get; set; }

        [JsonProperty("odata.type")]
        [NotMapped]
        public string OdataType { get; set; } = "Brizbee.Common.Security.Credentials";
    }
}
