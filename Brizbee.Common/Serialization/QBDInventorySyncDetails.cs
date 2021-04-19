using Brizbee.Common.Models;

namespace Brizbee.Common.Serialization
{
    public class QBDInventorySyncDetails
    {
        public QBDInventoryItem[] InventoryItems { get; set; }
        public QBDInventorySite[] InventorySites { get; set; }
        public QBDUnitOfMeasureSet[] UnitOfMeasureSets { get; set; }
    }
}
