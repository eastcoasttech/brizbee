using System.ComponentModel.DataAnnotations;

namespace Brizbee.Dashboard.Server.Serialization
{
    public class EmailSession
    {
        [Required(ErrorMessage = "Email address is required.")]
        public string EmailAddress { get; set; }

        [Required(ErrorMessage = "Password is required.")]
        [StringLength(128, ErrorMessage = "Password is too long (128 character limit).")]
        public string EmailPassword { get; set; }
    }
}
