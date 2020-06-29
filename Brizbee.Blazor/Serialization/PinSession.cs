using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace Brizbee.Blazor.Serialization
{
    public class PinSession
    {
        [Required(ErrorMessage = "PIN is required.")]
        public string UserPin { get; set; }

        [Required(ErrorMessage = "Organization code is required.")]
        public string OrganizationCode { get; set; }
    }
}
