﻿using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Brizbee.Dashboard.Models
{
    public class QBDInventoryConsumption
    {
        /// <summary>
        /// Id of the adjustment.
        /// </summary>
        [Key]
        public long Id { get; set; }

        /// <summary>
        /// Quantity of the inventory item to be adjusted.
        /// </summary>
        [Required]
        public int Quantity { get; set; }

        /// <summary>
        /// Unit of measure of the inventory item being adjusted.
        /// </summary>
        [StringLength(31)]
        public string UnitOfMeasure { get; set; }

        /// <summary>
        /// Moment when the inventory adjustment was made.
        /// </summary>
        [Required]
        [Column(TypeName = "datetime2")]
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// Id for the user who made the adjustment.
        /// </summary>
        [Required]
        public int CreatedByUserId { get; set; }

        /// <summary>
        /// The user who made the adjustment.
        /// </summary>
        [ForeignKey("CreatedByUserId")]
        public virtual User CreatedByUser { get; set; }

        /// <summary>
        /// Hostname of the machine where the adjustment was created.
        /// </summary>
        [Required]
        [StringLength(30)]
        public string Hostname { get; set; }

        /// <summary>
        /// Id of the inventory item in QuickBooks Desktop that was adjusted.
        /// </summary>
        [Required]
        public long QBDInventoryItemId { get; set; }

        /// <summary>
        /// The inventory item in QuickBooks Desktop that was adjusted.
        /// </summary>
        [ForeignKey("QBDInventoryItemId")]
        public virtual QBDInventoryItem QBDInventoryItem { get; set; }

        /// <summary>
        /// Id of the site in QuickBooks Desktop where the inventory item was adjusted.
        /// </summary>
        public long? QBDInventorySiteId { get; set; }

        /// <summary>
        /// The site in QuickBooks Desktop where the inventory item was adjusted.
        /// </summary>
        [ForeignKey("QBDInventorySiteId")]
        public virtual QBDInventorySite QBDInventorySite { get; set; }
        
        public long? QBDUnitOfMeasureSetId { get; set; }

        [ForeignKey("QBDUnitOfMeasureSetId")]
        public virtual QBDUnitOfMeasureSet QBDUnitOfMeasureSet { get; set; }

        /// <summary>
        /// Id of the organization to which the inventory adjustment belongs.
        /// </summary>
        [Required]
        public int OrganizationId { get; set; }

        /// <summary>
        /// The organization to which the inventory adjustment belongs.
        /// </summary>
        [ForeignKey("OrganizationId")]
        public virtual Organization Organization { get; set; }

        /// <summary>
        /// Id of the sync which records the sync with QuickBooks Desktop.
        /// </summary>
        public long? QBDInventoryConsumptionSyncId { get; set; }

        /// <summary>
        /// The sync which records the sync with QuickBooks Desktop.
        /// </summary>
        [ForeignKey("QBDInventoryConsumptionSyncId")]
        public virtual QBDInventoryConsumptionSync QBDInventoryConsumptionSync { get; set; }

        /// <summary>
        /// Id of the task which was being performed when the inventory was consumed.
        /// </summary>
        [Required]
        public int TaskId { get; set; }

        /// <summary>
        /// The task which was being performed when the inventory was consumed.
        /// </summary>
        [ForeignKey("TaskId")]
        public virtual Task Task { get; set; }
    }
}
