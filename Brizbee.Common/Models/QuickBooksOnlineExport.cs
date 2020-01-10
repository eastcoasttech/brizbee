using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace Brizbee.Common.Models
{
    public class QuickBooksOnlineExport
    {
        public string AccessToken { get; set; }

        public string AccessTokenExpiresAt { get; set; }

        public int? CommitId { get; set; }

        [ForeignKey("CommitId")]
        public virtual Commit Commit { get; set; }

        [Required]
        [Column(TypeName = "datetime2")]
        public DateTime CreatedAt { get; set; }

        public string CreatedTimeActivitiesIds { get; set; }

        [Key]
        public int Id { get; set; }

        [Column(TypeName = "datetime2")]
        public DateTime? InAt { get; set; }

        [Column(TypeName = "datetime2")]
        public DateTime? OutAt { get; set; }

        public string RefreshToken { get; set; }

        public string RefreshTokenExpiresAt { get; set; }

        [Required]
        public int UserId { get; set; }

        [ForeignKey("UserId")]
        public virtual User User { get; set; }
    }
}
