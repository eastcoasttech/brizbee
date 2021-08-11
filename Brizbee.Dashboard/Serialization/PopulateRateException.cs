using System;

namespace Brizbee.Dashboard.Serialization
{
    public class PopulateRateException
    {
        public string Option { get; set; }

        public string BasePayrollRateId { get; set; }

        public string AlternatePayrollRateId { get; set; }

        public string BaseServiceRateId { get; set; }

        public string AlternateServiceRateId { get; set; }

        public string RangeHour { get; set; }

        public string RangeMinute { get; set; }

        public string RangeMerdian { get; set; }

        public string CountHours { get; set; }

        public string CountMinutes { get; set; }

        public DateTime Date { get; set; }

        public string CalculatedType
        {
            get
            {
                if (Option == "Punches After" || Option == "Punches Before")
                {
                    return "range";
                }
                else if (Option == "After Hours/Minutes Per Day" || Option == "After Hours/Minutes in Range")
                {
                    return "count";
                }
                else if (Option == "Punches on Specific Date")
                {
                    return "date";
                }
                else
                {
                    return null;
                }
            }
        }

        public string CalculatedRangeDirection
        {
            get
            {
                if (Option == "Punches Before")
                {
                    return "before";
                }
                else if (Option == "Punches After")
                {
                    return "after";
                }
                else
                {
                    return null;
                }
            }
        }

        public int? CalculatedRangeMinutes
        {
            get
            {
                if (Option == "Punches Before" || Option == "Punches After")
                {
                    // 12 PM, 2PM, 3AM, 12AM
                    var rangeHour = int.Parse(RangeHour);

                    if (rangeHour == 12 && RangeMerdian == "AM")
                        rangeHour = 0;
                        
                    if (RangeMerdian == "PM" && rangeHour != 12)
                        rangeHour += 12;

                    var rangeMinute = int.Parse(RangeMinute);

                    return (rangeHour * 60) + rangeMinute;
                }
                else
                {
                    return null;
                }
            }
        }

        public string CalculatedCountScope
        {
            get
            {
                if (Option == "After Hours/Minutes Per Day")
                {
                    return "day";
                }
                else if (Option == "After Hours/Minutes in Range")
                {
                    return "total";
                }
                else
                {
                    return null;
                }
            }
        }

        public int? CalculatedCountMinutes
        {
            get
            {
                if (Option == "After Hours/Minutes Per Day" || Option == "After Hours/Minutes in Range")
                {
                    var countHours = int.Parse(CountHours);
                    var countMinutes = int.Parse(CountMinutes);

                    return (countHours * 60) + countMinutes;
                }
                else
                {
                    return null;
                }
            }
        }
    }
}
