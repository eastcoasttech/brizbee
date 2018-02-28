﻿using System;
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
        public DateTime CreatedAt { get; set; }

        [Key]
        public int Id { get; set; }

        [Required]
        public DateTime InAt { get; set; }

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

        public DateTime? OutAt { get; set; }

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