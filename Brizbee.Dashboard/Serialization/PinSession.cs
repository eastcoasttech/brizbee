using System.ComponentModel.DataAnnotations;

namespace Brizbee.Dashboard.Serialization
{
    public class PinSession
    {
        [Required(ErrorMessage = "PIN is required.")]
        public string UserPin { get; set; }

        [Required(ErrorMessage = "Organization code is required.")]
        public string OrganizationCode { get; set; }
    }
}
