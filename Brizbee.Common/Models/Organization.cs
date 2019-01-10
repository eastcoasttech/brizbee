﻿using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Brizbee.Common.Models
{
    public partial class Organization
    {
        [Required]
        [Column(TypeName = "datetime2")]
        public DateTime CreatedAt { get; set; }

        [Key]
        public int Id { get; set; }

        [Required]
        public string Name { get; set; }

        [Required]
        [MaxLength(8)]
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

        public string TimeZone { get; set; }
    }
}
