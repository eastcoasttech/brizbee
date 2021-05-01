using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Brizbee.Common.Models
{
    public class QBDInventoryItem
    {
        /// <summary>
        /// Id of the item.
        /// </summary>
        [Key]
        public long Id { get; set; }

        /// <summary>
        /// Name of the inventory item.
        /// </summary>
        [MaxLength(31)]
        public string Name { get; set; }

        /// <summary>
        /// Full name of the inventory item.
        /// </summary>
        [MaxLength(159)]
        public string FullName { get; set; }

        /// <summary>
        /// Manufacturer part number.
        /// </summary>
        [MaxLength(31)]
        public string ManufacturerPartNumber { get; set; }

        /// <summary>
        /// Bar code value of the item in QuickBooks Desktop.
        /// </summary>
        [MaxLength(50)]
        public string BarCodeValue { get; set; }

        /// <summary>
        /// ListID of the inventory item in QuickBooks Desktop.
        /// </summary>
        [MaxLength(20)]
        public string ListId { get; set; }

        /// <summary>
        /// Purchase description.
        /// </summary>
        [MaxLength(256)]
        public string PurchaseDescription { get; set; }

        /// <summary>
        /// Cost of the item to purchase.
        /// </summary>
        public decimal PurchaseCost { get; set; }

        /// <summary>
        /// Sales description.
        /// </summary>
        [MaxLength(256)]
        public string SalesDescription { get; set; }

        /// <summary>
        /// Price at which to sell the item.
        /// </summary>
        public decimal SalesPrice { get; set; }

        /// <summary>
        /// Name of the optional unit of measure set within QuickBooks.
        /// </summary>
        [StringLength(31)]
        public string QBDUnitOfMeasureSetFullName { get; set; }

        /// <summary>
        /// ListID of the optional unit of measure set within QuickBooks.
        /// </summary>
        [StringLength(20)]
        public string QBDUnitOfMeasureSetListId { get; set; }

        /// <summary>
        /// Id of the optional unit of measure set within QuickBooks Desktop.
        /// </summary>
        public long? QBDUnitOfMeasureSetId { get; set; }

        /// <summary>
        /// The optional unit of measure set within QuickBooks Desktop.
        /// </summary>
        [ForeignKey("QBDUnitOfMeasureSetId")]
        public virtual QBDUnitOfMeasureSet QBDUnitOfMeasureSet { get; set; }

        /// <summary>
        /// Id of the sync which records the sync with QuickBooks Desktop.
        /// </summary>
        [Required]
        public long QBDInventoryItemSyncId { get; set; }

        /// <summary>
        /// The sync which records the sync with QuickBooks Desktop.
        /// </summary>
        [ForeignKey("QBDInventoryItemSyncId")]
        public virtual QBDInventoryItemSync QBDInventoryItemSync { get; set; }

        /// <summary>
        /// Name of the Cost of Goods Sold account for the item within QuickBooks.
        /// </summary>
        [StringLength(31)]
        public string QBDCOGSAccountFullName { get; set; }

        /// <summary>
        /// ListID of the Cost of Goods Sold account for the item within QuickBooks.
        /// </summary>
        [StringLength(20)]
        public string QBDCOGSAccountListId { get; set; }

        /// <summary>
        /// Id of the organization to which the item belongs.
        /// </summary>
        [Required]
        public int OrganizationId { get; set; }

        /// <summary>
        /// Organization to which the item belongs.
        /// </summary>
        [ForeignKey("OrganizationId")]
        public virtual Organization Organization { get; set; }
    }
}
