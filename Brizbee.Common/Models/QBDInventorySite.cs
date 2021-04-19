using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Brizbee.Common.Models
{
    public class QBDInventorySite
    {
        /// <summary>
        /// Database id of the object.
        /// </summary>
        [Key]
        public long Id { get; set; }

        /// <summary>
        /// Name of the object in QuickBooks.
        /// </summary>
        [MaxLength(159)]
        public string Name { get; set; }

        /// <summary>
        /// ListID of the object in QuickBooks Desktop.
        /// </summary>
        [MaxLength(20)]
        public string ListId { get; set; }

        /// <summary>
        /// Whether or not the inventory site is active in QuickBooks.
        /// </summary>
        public bool IsActive { get; set; }

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
    }
}
