using System;
using System.ComponentModel.DataAnnotations;

namespace Brizbee.Common.Models
{
    public partial class Organization
    {
        [Required]
        public DateTime CreatedAt { get; set; }

        [Key]
        public int Id { get; set; }

        [Required]
        public string Name { get; set; }

        [Required]
        public string Code { get; set; }

        [Required]
        public string StripeCustomerId { get; set; }

        public string StripeSourceCardBrand { get; set; }

        public string StripeSourceCardExpirationMonth { get; set; }

        public string StripeSourceCardExpirationYear { get; set; }

        public string StripeSourceCardLast4 { get; set; }
        
        public string StripeSourceId { get; set; }

        [Required]
        public string StripeSubscriptionId { get; set; }
    }
}
