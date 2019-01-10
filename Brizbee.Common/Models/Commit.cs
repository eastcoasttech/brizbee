using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Brizbee.Common.Models
{
    public partial class Commit
    {
        [Required]
        [Column(TypeName = "datetime2")]
        public DateTime CreatedAt { get; set; }

        [Column(TypeName = "datetime2")]
        public DateTime? QuickBooksExportedAt { get; set; }

        [Key]
        public int Id { get; set; }

        [Required]
        [Column(TypeName = "datetime2")]
        public DateTime InAt { get; set; }
        
        [NotMapped]
        public string Name {
            get {
                return string.Format("{0} - {1}", InAt.ToString("d"), OutAt.ToString("d"));
            }
        }

        [Required]
        public int OrganizationId { get; set; }

        [ForeignKey("OrganizationId")]
        public virtual Organization Organization { get; set; }

        [Required]
        [Column(TypeName = "datetime2")]
        public DateTime OutAt { get; set; }

        public int PunchCount { get; set; }

        [Required]
        public int UserId { get; set; }

        [ForeignKey("UserId")]
        public virtual User User { get; set; }
    }
}
