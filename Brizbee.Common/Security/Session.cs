using Newtonsoft.Json;
using System.ComponentModel.DataAnnotations.Schema;

namespace Brizbee.Common.Security
{
    public class Session
    {
        public string EmailAddress { get; set; }
        public string EmailPassword { get; set; }
        public string Method { get; set; }
        public string PinOrganizationCode { get; set; }
        public string PinUserPin { get; set; }

        [JsonProperty("odata.type")]
        [NotMapped]
        public string OdataType { get; set; } = "Brizbee.Common.Security.Session";
    }
}
