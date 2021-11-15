using Brizbee.Core.Models;

namespace Brizbee.Core.Serialization
{
    public class QBDInventoryItemSyncDetails
    {
        public QBDInventoryItem[] InventoryItems { get; set; }
        public QBDInventorySite[] InventorySites { get; set; }
        public QBDUnitOfMeasureSet[] UnitOfMeasureSets { get; set; }
    }
}
