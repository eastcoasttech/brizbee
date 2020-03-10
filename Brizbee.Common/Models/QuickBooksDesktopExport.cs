using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace Brizbee.Common.Models
{
    public class QuickBooksDesktopExport
    {
        public int? CommitId { get; set; }

        [ForeignKey("CommitId")]
        public virtual Commit Commit { get; set; }

        [Required]
        [Column(TypeName = "datetime2")]
        public DateTime CreatedAt { get; set; }

        public string Log { get; set; }

        [Key]
        public int Id { get; set; }

        [Column(TypeName = "datetime2")]
        public DateTime? InAt { get; set; }

        [Column(TypeName = "datetime2")]
        public DateTime? OutAt { get; set; }

        [MaxLength(40)]
        public string QBProductName { get; set; }

        [MaxLength(10)]
        public string QBMajorVersion { get; set; }

        [MaxLength(10)]
        public string QBMinorVersion { get; set; }

        [MaxLength(10)]
        public string QBCountry { get; set; }

        [MaxLength(100)]
        public string QBSupportedQBXMLVersions { get; set; }

        [Required]
        public int UserId { get; set; }

        [ForeignKey("UserId")]
        public virtual User User { get; set; }
    }
}
