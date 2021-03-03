using System.ComponentModel.DataAnnotations;

namespace Brizbee.Dashboard.Serialization
{
    public class EmailSession
    {
        [Required(ErrorMessage = "Email address is required.")]
        public string EmailAddress { get; set; }

        [Required(ErrorMessage = "Password is required.")]
        public string EmailPassword { get; set; }
    }
}
