using System.Collections.Generic;

namespace Brizbee.Core.Serialization
{
    public class QuickBooksUnitOfMeasures
    {
        public QuickBooksUnitOfMeasure BaseUnit { get; set; }

        public List<QuickBooksUnitOfMeasure> RelatedUnits { get; set; }
    }
}
