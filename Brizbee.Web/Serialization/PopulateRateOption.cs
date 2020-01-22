using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Brizbee.Web.Serialization
{
    public class PopulateRateOption
    {
        public int? BasePayrollRateId { get; set; }

        public int? AlternatePayrollRateId { get; set; }

        public int? BaseServiceRateId { get; set; }

        public int? AlternateServiceRateId { get; set; }

        public string Type { get; set; } // count or range

        public string CountScope { get; set; } // day or total

        public int? CountMinute { get; set; } // 40 hours = 2,400 minutes

        public string RangeDirection { get; set; } // before or after

        public int? RangeMinutes { get; set; } // 7am = 420 minutes, 5:30pm = 1,050 minutes

        // Option 1 - After Hour (Hour=17)

        // Option 2 - Before Hour (Hour=5)

        // Option 3 - After Total Count (Count=40)

        // Option 4 - After Daily Count (Count=40)
    }
}