using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Brizbee.Services
{
    public class HourSplitter
    {
        public List<Tuple<DateTime, DateTime>> Results = new List<Tuple<DateTime, DateTime>>();
        private DateTime originalInAt;
        private DateTime originalOutAt;
        private int hourToSplit;

        public HourSplitter(DateTime inAt, DateTime outAt, int hour)
        {
            originalInAt = inAt;
            originalOutAt = outAt;
            hourToSplit = hour;

            // Start splitting
            split(originalInAt);
        }
        
        private void split(DateTime inAt)
        {
            // First, add an hour
            var adjustedOutAt = inAt.AddHours(1);

            // Then, move to the bottom of the hour
            adjustedOutAt = new DateTime(adjustedOutAt.Year, adjustedOutAt.Month,
                adjustedOutAt.Day, adjustedOutAt.Hour, 0, 0);

            // Finally, loop each hour in the duration to see if it is within the given hour,
            // as long as the rounded-up hour is not greater than the original out punch
            while (adjustedOutAt < originalOutAt)
            {
                if ((adjustedOutAt.Hour == hourToSplit)) // || (adjustedOutAt.Hour == 0)
                {
                    // Add the split duration to the result, and run this method
                    // again with the remaining duration
                    Results.Add(new Tuple<DateTime, DateTime>(inAt, new DateTime(adjustedOutAt.Year, adjustedOutAt.Month, adjustedOutAt.Day, adjustedOutAt.Hour, 59, 59).AddHours(-1)));
                    split(adjustedOutAt);

                    return;
                }
                else
                {
                    // Loop again, adding another hour to the out punch
                    adjustedOutAt = adjustedOutAt.AddHours(1);
                }
            }

            // After the out punch is calculated, add the duration
            if (adjustedOutAt > originalOutAt)
            {
                Results.Add(new Tuple<DateTime, DateTime>(inAt, originalOutAt));
            }
            else
            {
                Results.Add(new Tuple<DateTime, DateTime>(inAt, new DateTime(adjustedOutAt.Year, adjustedOutAt.Month, adjustedOutAt.Day, adjustedOutAt.Hour, 59, 59).AddHours(-1)));
            }
        }
    }
}