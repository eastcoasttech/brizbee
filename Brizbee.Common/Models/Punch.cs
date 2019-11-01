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
        public double Minutes
        {
            get
            {
                if (this.OutAt.HasValue)
                {
                    return (this.OutAt.Value - this.InAt).TotalMinutes;
                }
                else
                {
                    return 0.0;
                }
            }
        }

        [Column(TypeName = "datetime2")]
        public DateTime? OutAt { get; set; }

        public string OutAtTimeZone { get; set; }

        public string SourceForInAt { get; set; }

        public string SourceForOutAt { get; set; }

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
