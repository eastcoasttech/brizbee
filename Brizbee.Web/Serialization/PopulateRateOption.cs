//
//  PopulateRateOption.cs
//  BRIZBEE API
//
//  Copyright (C) 2020 East Coast Technology Services, LLC
//
//  This file is part of the BRIZBEE API.
//
//  This program is free software: you can redistribute it and/or modify
//  it under the terms of the GNU Affero General Public License as
//  published by the Free Software Foundation, either version 3 of the
//  License, or (at your option) any later version.
//
//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//  GNU Affero General Public License for more details.
//
//  You should have received a copy of the GNU Affero General Public License
//  along with this program.  If not, see <https://www.gnu.org/licenses/>.
//

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

        public string Type { get; set; } // count, range, or date

        public string CountScope { get; set; } // day or total

        public int? CountMinute { get; set; } // 40 hours = 2,400 minutes

        public string RangeDirection { get; set; } // before or after

        public int? RangeMinutes { get; set; } // 7am = 420 minutes, 5:30pm = 1,050 minutes

        public DateTime Date { get; set; } // on specific date

        public string DayOfWeek { get; set; } // on day of week

        public int Order { get; set; }

        // Option 1 - After Hour (Hour=17)

        // Option 2 - Before Hour (Hour=5)

        // Option 3 - After Total Count (Count=40)

        // Option 4 - After Daily Count (Count=40)
    }
}