using System;
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
        /// Hostname of the machine with QuickBooks.
        /// </summary>
        [StringLength(15)]
        public string Hostname { get; set; }

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
        [StringLength(100)]
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
        /// Company file name collected from QuickBooks host details.
        /// </summary>
        [Required]
        [StringLength(200)]
        public string HostCompanyFileName { get; set; }

        /// <summary>
        /// Path to the company file collected from QuickBooks host details.
        /// </summary>
        [Required]
        [StringLength(500)]
        public string HostCompanyFilePath { get; set; }

        /// <summary>
        /// Indicates the number of consumptions that were synced.
        /// </summary>
        [Required]
        public int ConsumptionsCount { get; set; }

        /// <summary>
        /// Comma-separated list of TxnIDs from the added transactions in QuickBooks.
        /// </summary>
        public string TxnIDs { get; set; }
        
        [NotMapped]
        public string Name
        {
            get
            {
                return $"{CreatedAt.ToShortDateString()} - Sync # {Id} - {RecordingMethod} - {ValueMethod} - {HostCompanyFileName}";
            }
        }

        /// <summary>
        /// Moment when the sync was reversed.
        /// </summary>
        [Column(TypeName = "datetime2")]
        public DateTime? ReversedAt { get; set; }

        /// <summary>
        /// Id for the user who reversed the sync.
        /// </summary>
        public int? ReversedByUserId { get; set; }

        /// <summary>
        /// The user who reversed the sync.
        /// </summary>
        [ForeignKey("ReversedByUserId")]
        public virtual User ReversedByUser { get; set; }
    }
}
