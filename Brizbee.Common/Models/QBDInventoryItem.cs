using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Brizbee.Common.Models
{
    public class QBDInventoryItem
    {
        /// <summary>
        /// Name of the inventory item.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Full name of the inventory item.
        /// </summary>
        public string FullName { get; set; }

        /// <summary>
        /// Manufacturer part number.
        /// </summary>
        public string ManufacturerPartNumber { get; set; }

        /// <summary>
        /// Bar code value of the item in QuickBooks Desktop.
        /// </summary>
        public string BarCodeValue { get; set; }

        /// <summary>
        /// ListID of the inventory item in QuickBooks Desktop.
        /// </summary>
        public string ListId { get; set; }

        /// <summary>
        /// Purchase description.
        /// </summary>
        public string PurchaseDescription { get; set; }

        /// <summary>
        /// Sales description.
        /// </summary>
        public string SalesDescription { get; set; }

        /// <summary>
        /// Id of the sync which records the sync with QuickBooks Desktop.
        /// </summary>
        [Required]
        public int QBDInventoryItemSyncId { get; set; }

        /// <summary>
        /// The sync which records the sync with QuickBooks Desktop.
        /// </summary>
        [ForeignKey("QBDInventoryItemSyncId")]
        public virtual QBDInventoryItemSync QBDInventoryItemSync { get; set; }
    }
}
