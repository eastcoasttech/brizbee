﻿using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Brizbee.Common.Models
{
    public class QBDInventoryConsumptionSync
    {
        /// <summary>
        /// Id of the sync.
        /// </summary>
        [Key]
        public long Id { get; set; }

        /// <summary>
        /// Moment when the sync was performed.
        /// </summary>
        [Required]
        [Column(TypeName = "datetime2")]
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// Id for the user who made the sync.
        /// </summary>
        [Required]
        public int CreatedByUserId { get; set; }

        /// <summary>
        /// The user who made the sync.
        /// </summary>
        [ForeignKey("CreatedByUserId")]
        public virtual User CreatedByUser { get; set; }

        /// <summary>
        /// Id of the organization to which the sync belongs.
        /// </summary>
        [Required]
        public int OrganizationId { get; set; }

        /// <summary>
        /// The organization to which the sync belongs.
        /// </summary>
        [ForeignKey("OrganizationId")]
        public virtual Organization Organization { get; set; }

        /// <summary>
        /// Method used to record the consumption: Inventory Adjustment or Sales Receipt
        /// </summary>
        [Required]
        [StringLength(25)]
        public string RecordingMethod { get; set; }

        /// <summary>
        /// Method used to value the inventory: Purchase Cost, Sales Price, or Zero
        /// </summary>
        [StringLength(25)]
        public string ValueMethod { get; set; }

        /// <summary>
        /// Product name collected from QuickBooks host details.
        /// </summary>
        [Required]
        [StringLength(50)]
        public string HostProductName { get; set; }

        /// <summary>
        /// Major version collected from QuickBooks host details.
        /// </summary>
        [Required]
        [StringLength(10)]
        public string HostMajorVersion { get; set; }

        /// <summary>
        /// Minor version collected from QuickBooks host details.
        /// </summary>
        [Required]
        [StringLength(10)]
        public string HostMinorVersion { get; set; }

        /// <summary>
        /// Country collected from QuickBooks host details.
        /// </summary>
        [Required]
        [StringLength(10)]
        public string HostCountry { get; set; }

        /// <summary>
        /// Supported QBXML version collected from QuickBooks host details.
        /// </summary>
        [Required]
        [StringLength(100)]
        public string HostSupportedQBXMLVersion { get; set; }

        /// <summary>
        /// Indicates the number of consumptions that were synced.
        /// </summary>
        [Required]
        public int ConsumptionsCount { get; set; }
    }
}
