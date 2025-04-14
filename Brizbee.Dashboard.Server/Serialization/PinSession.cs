using System.ComponentModel.DataAnnotations;

namespace Brizbee.Dashboard.Server.Serialization
{
    public class PinSession
    {
        [Required(ErrorMessage = "PIN is required.")]
        [StringLength(8, ErrorMessage = "PIN is too long (8 character limit).")]
        public string UserPin { get; set; }

        [Required(ErrorMessage = "Organization code is required.")]
        [StringLength(8, ErrorMessage = "Organization code is too long (8 character limit).")]
        public string OrganizationCode { get; set; }
    }
}
