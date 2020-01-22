using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Brizbee.Common.Models
{
    public partial class Punch
    {
        public int? CommitId { get; set; }

        [ForeignKey("CommitId")]
        public virtual Commit Commit { get; set; }

        [Required]
        [Column(TypeName = "datetime2")]
        public DateTime CreatedAt { get; set; }

        [Required]
        public Guid Guid { get; set; }

        [Key]
        public int Id { get; set; }

        [Required]
        [Column(TypeName = "datetime2")]
        public DateTime InAt { get; set; }

        public string InAtTimeZone { get; set; }

        [MaxLength(20)]
        public string LatitudeForInAt { get; set; }

        [MaxLength(20)]
        public string LongitudeForInAt { get; set; }

        [MaxLength(20)]
        public string LatitudeForOutAt { get; set; }

        [MaxLength(20)]
        public string LongitudeForOutAt { get; set; }

        [NotMapped]
        public int Minutes
        {
            get
            {
                if (this.OutAt.HasValue)
                {
                    return Convert.ToInt32((this.OutAt.Value - this.InAt).TotalMinutes);
                }
                else
                {
                    return 0;
                }
            }
        }

        [Column(TypeName = "datetime2")]
        public DateTime? OutAt { get; set; }

        public string OutAtTimeZone { get; set; }

        public string SourceForInAt { get; set; }

        public string SourceForOutAt { get; set; }

        [MaxLength(12)]
        public string InAtSourceHardware { get; set; } // Web, Mobile, Phone, Dashboard

        [MaxLength(30)]
        public string InAtSourceHostname { get; set; }

        [MaxLength(30)]
        public string InAtSourceIpAddress { get; set; }

        [MaxLength(30)]
        public string InAtSourceOperatingSystem { get; set; }

        [MaxLength(20)]
        public string InAtSourceOperatingSystemVersion { get; set; }

        [MaxLength(30)]
        public string InAtSourceBrowser { get; set; }

        [MaxLength(20)]
        public string InAtSourceBrowserVersion { get; set; }

        [MaxLength(30)]
        public string InAtSourcePhoneNumber { get; set; }

        [MaxLength(12)]
        public string OutAtSourceHardware { get; set; } // Web, Mobile, Phone, Dashboard

        [MaxLength(30)]
        public string OutAtSourceHostname { get; set; }

        [MaxLength(30)]
        public string OutAtSourceIpAddress { get; set; }

        [MaxLength(30)]
        public string OutAtSourceOperatingSystem { get; set; }

        [MaxLength(20)]
        public string OutAtSourceOperatingSystemVersion { get; set; }

        [MaxLength(30)]
        public string OutAtSourceBrowser { get; set; }

        [MaxLength(20)]
        public string OutAtSourceBrowserVersion { get; set; }

        [MaxLength(30)]
        public string OutAtSourcePhoneNumber { get; set; }

        public int? ServiceRateId { get; set; } // Populated later by administrators

        [ForeignKey("ServiceRateId")]
        public virtual Rate ServiceRate { get; set; }

        public int? PayrollRateId { get; set; } // Populated later by administrators

        [ForeignKey("PayrollRateId")]
        public virtual Rate PayrollRate { get; set; }

        [Required]
        public int TaskId { get; set; }

        [ForeignKey("TaskId")]
        public virtual Task Task { get; set; }

        [Required]
        public int UserId { get; set; }

        [ForeignKey("UserId")]
        public virtual User User { get; set; }
    }
}
