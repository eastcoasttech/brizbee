using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace Brizbee.Common.Models
{
    public class QuickBooksOnlineExport
    {
        public int? CommitId { get; set; }

        [ForeignKey("CommitId")]
        public virtual Commit Commit { get; set; }

        [Required]
        [Column(TypeName = "datetime2")]
        public DateTime CreatedAt { get; set; }

        [Required]
        public int CreatedByUserId { get; set; }

        [ForeignKey("CreatedByUserId")]
        public virtual User CreatedByUser { get; set; }

        public string CreatedTimeActivitiesIds { get; set; }

        [Key]
        public int Id { get; set; }

        [Column(TypeName = "datetime2")]
        public DateTime? InAt { get; set; }

        [Column(TypeName = "datetime2")]
        public DateTime? OutAt { get; set; }

        [Column(TypeName = "datetime2")]
        public DateTime? ReversedAt { get; set; }

        public int? ReversedByUserId { get; set; }

        [ForeignKey("ReversedByUserId")]
        public virtual User ReversedByUser { get; set; }
    }
}
