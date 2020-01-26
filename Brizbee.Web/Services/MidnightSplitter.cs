using Brizbee.Common.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Brizbee.Web.Services
{
    public class MidnightSplitter
    {
        public List<Punch> Split(Punch originalPunch) // 10/01/2019 06:00:00 - 10/02/2019 11:00:00
        {
            var processed = new List<Punch>();

            var originalMidnight = new DateTime(originalPunch.InAt.Year, originalPunch.InAt.Month, originalPunch.InAt.Day, 0, 0, 0, 0).AddDays(1); // 10/02/2019 00:00:00

            // Punch out extends beyond midnight into the next day
            if (originalMidnight < originalPunch.OutAt.Value)
            {
                var originalInAt = originalPunch.InAt;
                var originalOutAt = originalPunch.OutAt.Value;

                var adjustedInAt = originalInAt; // Beginning of range
                var adjustedOutAt = new DateTime(originalInAt.Year, originalInAt.Month, originalInAt.Day, 23, 59, 59, 999); // Last second of same day

                var newInAt = new DateTime(originalInAt.Year, originalInAt.Month, originalInAt.Day, 0, 0, 0, 000).AddDays(1); // Midnight on next day
                var newOutAt = originalOutAt; // End of range, not safe, could extend past midnight

                var adjustedPunch = new Punch() // Processed
                {
                    InAt = adjustedInAt,
                    OutAt = adjustedOutAt
                };
                var newPunch = new Punch() // Not safe, must be checked again
                {
                    InAt = newInAt,
                    OutAt = newOutAt
                };

                // The adjusted punch is processed
                processed.Add(adjustedPunch);

                // Check if new punch extends beyond midnight into the next day
                var newMidnight = new DateTime(newPunch.InAt.Year, newPunch.InAt.Month, newPunch.InAt.Day, 0, 0, 0, 0).AddDays(1); // 10/03/2019 00:00:00
                if (newMidnight < newPunch.OutAt.Value)
                {
                    processed.AddRange(Split(newPunch));
                }
                else
                {
                    processed.Add(newPunch);
                }
            }
            else
            {
                processed.Add(originalPunch);
            }

            return processed;
        }
    }
}