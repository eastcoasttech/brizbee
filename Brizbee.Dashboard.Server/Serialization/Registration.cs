using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace Brizbee.Dashboard.Server.Serialization
{
    public class Registration
    {
        [Required(ErrorMessage = "Company name is required.", AllowEmptyStrings = false)]
        public string Organization { get; set; }

        [Required(ErrorMessage = "Your name is required.", AllowEmptyStrings = false)]
        public string Name { get; set; }

        [Required(ErrorMessage = "Email Address is required.", AllowEmptyStrings = false)]
        public string EmailAddress { get; set; }

        [Required(ErrorMessage = "Password is required.", AllowEmptyStrings = false)]
        public string Password { get; set; }

        [Required(ErrorMessage = "Time Zone is required.", AllowEmptyStrings = false)]
        public string TimeZone { get; set; }

        [Required(ErrorMessage = "Plan Selection is required.", AllowEmptyStrings = false)]
        public int PlanId { get; set; }
    }
}
