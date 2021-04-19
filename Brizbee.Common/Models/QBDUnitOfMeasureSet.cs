using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Brizbee.Common.Models
{
    /// <summary>
    /// Represents a Unit of Measure Set in QuickBooks Desktop;
    /// </summary>
    public class QBDUnitOfMeasureSet
    {
        /// <summary>
        /// Database id of the object.
        /// </summary>
        [Key]
        public long Id { get; set; }

        /// <summary>
        /// Name of the object in QuickBooks.
        /// </summary>
        [MaxLength(31)]
        public string Name { get; set; }

        /// <summary>
        /// ListID of the object in QuickBooks.
        /// </summary>
        [MaxLength(20)]
        public string ListId { get; set; }

        /// <summary>
        /// Type of the unit of measure in QuickBooks, potentially a custom value.
        /// </summary>
        [MaxLength(255)]
        public string UnitOfMeasureType { get; set; }

        /// <summary>
        /// Whether or not the unit of measure is active in QuickBooks.
        /// </summary>
        public bool IsActive { get; set; }

        /// <summary>
        /// Name of the object's base unit in QuickBooks.
        /// </summary>
        [MaxLength(31)]
        public string BaseUnitName { get; set; }

        /// <summary>
        /// Abbreviation for the object's base unit in QuickBooks.
        /// </summary>
        [MaxLength(31)]
        public string BaseUnitAbbreviation { get; set; }

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
