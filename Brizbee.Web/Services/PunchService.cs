using Brizbee.Common.Models;
using Brizbee.Web.Serialization;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Web;

namespace Brizbee.Web.Services
{
    public class PunchService : IDisposable
    {
        private BrizbeeWebContext db = new BrizbeeWebContext();
        private JsonSerializerSettings settings = new JsonSerializerSettings()
        {
            NullValueHandling = NullValueHandling.Ignore,
            StringEscapeHandling = StringEscapeHandling.EscapeHtml,
            ReferenceLoopHandling = ReferenceLoopHandling.Ignore
        };

        /// <summary>
        /// Disposes of the database connection.
        /// </summary>
        public void Dispose()
        {
            db.Dispose();
        }

        public void Populate(PopulateRateOptions rateOptions, User currentUser)
        {
            var inAt = rateOptions.InAt;
            var outAt = rateOptions.OutAt;
            var options = rateOptions.Options.ToList().OrderBy(o => o.Order);
            var punches = db.Punches
                .Include("Task")
                .Where(p => p.InAt >= inAt && p.OutAt.HasValue && p.OutAt.Value <= outAt)
                .ToList();
            var userIds = punches
                .GroupBy(p => p.UserId)
                .Select(g => g.Key)
                .ToArray();

            foreach (var punch in punches)
            {
                // Set all the rates to the default for the task
                punch.PayrollRateId = punch.Task.BasePayrollRateId;
                punch.ServiceRateId = punch.Task.BaseServiceRateId;
            }

            foreach (var option in options)
            {
                string rate;
                int baseRateId;
                int alternateRateId;

                if (option.AlternatePayrollRateId.HasValue)
                {
                    // populate payroll rates
                    rate = "payroll";
                    baseRateId = option.BasePayrollRateId.Value;
                    alternateRateId = option.AlternatePayrollRateId.Value;
                }
                else if (option.AlternateServiceRateId.HasValue)
                {
                    // populate service rates
                    rate = "service";
                    baseRateId = option.BaseServiceRateId.Value;
                    alternateRateId = option.AlternateServiceRateId.Value;
                }
                else
                {
                    throw new Exception("Must provide an alternate payroll or service id.");
                }

                switch (option.Type)
                {
                    case "count":

                        if (option.CountScope == "day")
                        {
                            // Populates beyond the number of minutes per day
                            PopulateForCountOfMinutesPerDay(
                                punches,
                                userIds,
                                option.CountMinute.Value,
                                baseRateId,
                                alternateRateId,
                                rate);
                        }
                        else if (option.CountScope == "total")
                        {
                            // Populates beyond the number of total minutes in the range
                            PopulateForCountOfTotalMinutes(
                                punches,
                                userIds,
                                option.CountMinute.Value,
                                baseRateId,
                                alternateRateId,
                                rate);
                        }

                        break;
                    case "range":

                        if (option.RangeDirection == "before")
                        {
                            // Populates before the number of minutes in the day
                            PopulateBasedOnRangeBeforeOrAfter(
                                punches,
                                userIds,
                                option.RangeMinutes.Value,
                                baseRateId,
                                alternateRateId,
                                "before",
                                rate);
                        }
                        else if (option.RangeDirection == "after")
                        {
                            // Populates after the number of minutes in the day
                            PopulateBasedOnRangeBeforeOrAfter(
                                punches,
                                userIds,
                                option.RangeMinutes.Value,
                                baseRateId,
                                alternateRateId,
                                "after",
                                rate);
                        }

                        break;
                }
            }

            foreach (var punch in punches.OrderBy(p => p.InAt))
            {
                var payrollRate = db.Rates.Find(punch.PayrollRateId);
                var serviceRate = db.Rates.Find(punch.ServiceRateId);
                Trace.TraceInformation(string.Format("{0} for {1} minutes    Payroll: {2}    Service: {3}", punch.InAt.ToString("yyyy-MM-dd HH:mm"), punch.Minutes, payrollRate.Name, serviceRate.Name));
            }

            // Save in a single transaction, so either all will fail or succeed
            //db.SaveChanges();
        }

        private void PopulateForCountOfMinutesPerDay(List<Punch> punches, int[] userIds, int minutesOfDay, int baseRateId, int alternateRateId, string rate = "payroll")
        {
            foreach (var userId in userIds)
            {
                var filtered = punches
                    .Where(p => p.UserId == userId)
                    .OrderBy(p => p.InAt);
                var dates = filtered
                    .GroupBy(p => p.InAt.Date)
                    .Select(g => new {
                        Date = g.Key
                    })
                    .ToList();

                // Loop the punches by date and populate the alternate rate
                foreach (var date in dates)
                {
                    var punchesForDay = filtered
                        .Where(p => p.InAt.Date == date.Date)
                        .ToList();
                    var count = 0;
                    foreach (var punch in punchesForDay)
                    {
                        count += punch.Minutes;

                        if (count > minutesOfDay)
                        {
                            if (rate == "payroll")
                            {
                                // Can only set alternate rates for the matching base rate
                                if (punch.Task.BasePayrollRateId == baseRateId)
                                {
                                    punch.PayrollRateId = alternateRateId;
                                    db.Punches.Attach(punch);
                                }
                            }
                            else if (rate == "service")
                            {
                                // Can only set alternate rates for the matching base rate
                                if (punch.Task.BaseServiceRateId == baseRateId)
                                {
                                    punch.ServiceRateId = alternateRateId;
                                    db.Punches.Attach(punch);
                                }
                            }
                        }
                    }
                }
            }
        }

        private void PopulateForCountOfTotalMinutes(List<Punch> punches, int[] userIds, int totalMinutes, int baseRateId, int alternateRateId, string rate = "payroll")
        {
            foreach (var userId in userIds)
            {
                // Loop the punches and populate the alternate rate
                var filtered = punches
                    .Where(p => p.UserId == userId)
                    .OrderBy(p => p.InAt);
                var count = 0;
                foreach (var punch in filtered)
                {
                    count += punch.Minutes;

                    if (count > totalMinutes)
                    {
                        if (rate == "payroll")
                        {
                            // Can only set alternate rates for the matching base rate
                            if (punch.Task.BasePayrollRateId == baseRateId)
                            {
                                punch.PayrollRateId = alternateRateId;
                                db.Punches.Attach(punch);
                            }
                        }
                        else if (rate == "service")
                        {
                            // Can only set alternate rates for the matching base rate
                            if (punch.Task.BaseServiceRateId == baseRateId)
                            {
                                punch.ServiceRateId = alternateRateId;
                                db.Punches.Attach(punch);
                            }
                        }
                    }
                }
            }
        }

        private void PopulateBasedOnRangeBeforeOrAfter(List<Punch> punches, int[] userIds, int minuteOfDay, int baseRateId, int alternateRateId, string beforeOrafter = "before", string rate = "payroll")
        {
            foreach (var userId in userIds)
            {
                var filtered = punches
                    .Where(p => p.UserId == userId)
                    .OrderBy(p => p.InAt);
                foreach (var punch in filtered)
                {
                    var minuteOfPunch = (punch.InAt.Hour * 60) + punch.InAt.Minute;
                    switch (beforeOrafter)
                    {
                        case "before":
                            if (minuteOfPunch <= minuteOfDay)
                            {
                                if (rate == "payroll")
                                {
                                    // Can only set alternate rates for the matching base rate
                                    if (punch.Task.BasePayrollRateId == baseRateId)
                                    {
                                        punch.PayrollRateId = alternateRateId;
                                        db.Punches.Attach(punch);
                                    }
                                }
                                else if (rate == "service")
                                {
                                    // Can only set alternate rates for the matching base rate
                                    if (punch.Task.BaseServiceRateId == baseRateId)
                                    {
                                        punch.ServiceRateId = alternateRateId;
                                        db.Punches.Attach(punch);
                                    }
                                }
                            }
                            break;
                        case "after":
                            if (minuteOfPunch >= minuteOfDay)
                            {
                                if (rate == "payroll")
                                {
                                    // Can only set alternate rates for the matching base rate
                                    if (punch.Task.BasePayrollRateId == baseRateId)
                                    {
                                        punch.PayrollRateId = alternateRateId;
                                        db.Punches.Attach(punch);
                                    }
                                }
                                else if (rate == "service")
                                {
                                    // Can only set alternate rates for the matching base rate
                                    if (punch.Task.BaseServiceRateId == baseRateId)
                                    {
                                        punch.ServiceRateId = alternateRateId;
                                        db.Punches.Attach(punch);
                                    }
                                }
                            }
                            break;
                    }
                }
            }
        }
    

        // Should split at midnight for all punches
        //public void SplitPunches(List<Punch> punches)
        //{
        //    var processed = new List<Punch>();

        //    foreach (var punch in punches)
        //    {
        //        var midnight = new DateTime(punch.InAt.Year, punch.InAt.Month, punch.InAt.Day, 0, 0, 0, 0).AddDays(1);

        //        // 10/1 6am - 10/2 2am (20 hours)

        //        // Punch out extends beyond midnight, into the next day
        //        if (midnight > punch.OutAt)
        //        {
        //            var adjusted = SplitAtMidnight(punch);

        //            // Adjusted could still extend past midnight
        //        }
        //        else
        //        {
        //            processed.Add(punch);
        //        }
        //    }
        //}

        public List<Punch> SplitAtMidnight(DateTime inAt, DateTime outAt, int[] userIds)
        {
            var punches = db.Punches
                .AsNoTracking()
                .Where(p => userIds.Contains(p.UserId))
                .Where(p => p.InAt >= inAt && p.OutAt.HasValue && p.OutAt.Value <= outAt)
                .OrderBy(p => p.UserId)
                .ThenBy(p => p.InAt)
                .ToList();

            var raw = JsonConvert.SerializeObject(punches, settings);
            Trace.TraceInformation(raw);

            var processed = new List<Punch>();
            foreach (var punch in punches)
            {
                var splitter = new MidnightSplitter();
                processed.AddRange(splitter.Split(originalPunch: punch));
            }
            var ordered = processed
                .OrderBy(p => p.UserId)
                .ThenBy(p => p.InAt)
                .ToList();

            //foreach (var punch in processed)
            //{
            //    Trace.TraceInformation(string.Format("{0} thru {1}", punch.InAt.ToString("yyyy-MM-dd HH:mm:ss.fff"), punch.OutAt.Value.ToString("yyyy-MM-dd HH:mm:ss.fff")));
            //}

            return ordered;
        }

        //private Tuple<Punch, Punch> SplitAtMidnight(Punch originalPunch)
        //{
        //    var originalInAt = originalPunch.InAt;
        //    var originalOutAt = originalPunch.OutAt.Value;

        //    var adjustedInAt = originalInAt; // Beginning of range
        //    var adjustedOutAt = new DateTime(originalInAt.Year, originalInAt.Month, originalInAt.Day, 23, 59, 59, 999); // Last second of same day

        //    var newInAt = new DateTime(originalInAt.Year, originalInAt.Month, originalInAt.Day, 0, 0, 0, 000).AddDays(1); // Midnight on next day
        //    var newOutAt = originalOutAt; // End of range, could extend past midnight

        //    var adjustedPunch = new Punch()
        //    {
        //        InAt = adjustedInAt,
        //        OutAt = adjustedOutAt
        //    };
        //    var newPunch = new Punch()
        //    {
        //        InAt = newInAt,
        //        OutAt = newOutAt
        //    };

        //    return new Tuple<Punch, Punch>(adjustedPunch, newPunch);
        //}

        
        public List<Punch> SplitAtMinute(List<Punch> punches, int[] userIds, int? minuteOfDay = null, int? minutesPerDay = null, int? minutesPerSpan = null)
        {
            var processed = new List<Punch>();

            foreach (var userId in userIds)
            {
                var filtered = punches
                    .Where(p => p.UserId == userId)
                    .OrderBy(p => p.InAt)
                    .ToList();

                if (minuteOfDay.HasValue)
                {
                    var dates = filtered
                        .GroupBy(p => p.InAt.Date)
                        .Select(g => new
                        {
                            Date = g.Key
                        });

                    foreach (var date in dates)
                    {
                        var punchesForDay = filtered
                            .Where(p => p.InAt.Date == date.Date)
                            .ToList();

                        foreach (var punch in punchesForDay)
                        {
                            var midnight = new DateTime(punch.InAt.Year, punch.InAt.Month, punch.InAt.Day, 0, 0, 0, 0);

                            var spanInAt = punch.InAt.Subtract(midnight);
                            var spanOutAt = punch.OutAt.Value.Subtract(midnight);

                            var minuteOfInAt = spanInAt.TotalMinutes;//(punch.InAt.Hour * 60) + punch.InAt.Minute;
                            var minuteOfOutAt = spanOutAt.TotalMinutes;//(punch.OutAt.Value.Hour * 60) + punch.OutAt.Value.Minute;

                            // Example punch is 6:00:00.000am thru 10:00:59.999am. Split at 7am.
                            // So if 7am is 420 minutes, check if 420 minutes is between InAt and OutAt.
                            if (minuteOfInAt < minuteOfDay && minuteOfOutAt > minuteOfDay) // 360 > 420 && 600 > 420
                            {
                                var originalInAt = punch.InAt;
                                var originalOutAt = punch.OutAt.Value;

                                var adjustedInAt = originalInAt;
                                var adjustedOutAt = new DateTime(punch.InAt.Year, punch.InAt.Month, punch.InAt.Day, 0, 0, 0, 0).AddMinutes(minuteOfDay.Value);

                                var newInAt = new DateTime(punch.InAt.Year, punch.InAt.Month, punch.InAt.Day, 0, 0, 0, 0).AddMinutes(minuteOfDay.Value);
                                var newOutAt = originalOutAt;

                                var adjustedPunch = new Punch()
                                {
                                    InAt = adjustedInAt,
                                    OutAt = adjustedOutAt,
                                    Guid = Guid.NewGuid(),
                                    CreatedAt = DateTime.UtcNow,
                                    LatitudeForInAt = punch.LatitudeForInAt,
                                    LatitudeForOutAt = punch.LatitudeForOutAt,
                                    LongitudeForInAt = punch.LongitudeForInAt,
                                    LongitudeForOutAt = punch.LongitudeForOutAt,
                                    InAtSourceBrowser = punch.InAtSourceBrowser,
                                    InAtSourceBrowserVersion = punch.InAtSourceBrowserVersion,
                                    InAtSourceHardware = punch.InAtSourceHardware,
                                    InAtSourceHostname = punch.InAtSourceHostname,
                                    InAtSourceIpAddress = punch.InAtSourceIpAddress,
                                    InAtSourceOperatingSystem = punch.InAtSourceOperatingSystem,
                                    InAtSourceOperatingSystemVersion = punch.InAtSourceOperatingSystemVersion,
                                    InAtSourcePhoneNumber = punch.InAtSourcePhoneNumber,
                                    InAtTimeZone = punch.InAtTimeZone,
                                    SourceForInAt = punch.SourceForInAt,
                                    OutAtSourceBrowser = punch.OutAtSourceBrowser,
                                    OutAtSourceBrowserVersion = punch.OutAtSourceBrowserVersion,
                                    OutAtSourceHardware = punch.OutAtSourceHardware,
                                    OutAtSourceHostname = punch.OutAtSourceHostname,
                                    OutAtSourceIpAddress = punch.OutAtSourceIpAddress,
                                    OutAtSourceOperatingSystem = punch.OutAtSourceOperatingSystem,
                                    OutAtSourceOperatingSystemVersion = punch.OutAtSourceOperatingSystemVersion,
                                    OutAtSourcePhoneNumber = punch.OutAtSourcePhoneNumber,
                                    OutAtTimeZone = punch.OutAtTimeZone,
                                    SourceForOutAt = punch.SourceForOutAt,
                                    TaskId = punch.TaskId,
                                    UserId = punch.UserId
                                };

                                var newPunch = new Punch()
                                {
                                    InAt = newInAt,
                                    OutAt = newOutAt,
                                    Guid = Guid.NewGuid(),
                                    CreatedAt = DateTime.UtcNow,
                                    LatitudeForInAt = punch.LatitudeForInAt,
                                    LatitudeForOutAt = punch.LatitudeForOutAt,
                                    LongitudeForInAt = punch.LongitudeForInAt,
                                    LongitudeForOutAt = punch.LongitudeForOutAt,
                                    InAtSourceBrowser = punch.InAtSourceBrowser,
                                    InAtSourceBrowserVersion = punch.InAtSourceBrowserVersion,
                                    InAtSourceHardware = punch.InAtSourceHardware,
                                    InAtSourceHostname = punch.InAtSourceHostname,
                                    InAtSourceIpAddress = punch.InAtSourceIpAddress,
                                    InAtSourceOperatingSystem = punch.InAtSourceOperatingSystem,
                                    InAtSourceOperatingSystemVersion = punch.InAtSourceOperatingSystemVersion,
                                    InAtSourcePhoneNumber = punch.InAtSourcePhoneNumber,
                                    InAtTimeZone = punch.InAtTimeZone,
                                    SourceForInAt = punch.SourceForInAt,
                                    OutAtSourceBrowser = punch.OutAtSourceBrowser,
                                    OutAtSourceBrowserVersion = punch.OutAtSourceBrowserVersion,
                                    OutAtSourceHardware = punch.OutAtSourceHardware,
                                    OutAtSourceHostname = punch.OutAtSourceHostname,
                                    OutAtSourceIpAddress = punch.OutAtSourceIpAddress,
                                    OutAtSourceOperatingSystem = punch.OutAtSourceOperatingSystem,
                                    OutAtSourceOperatingSystemVersion = punch.OutAtSourceOperatingSystemVersion,
                                    OutAtSourcePhoneNumber = punch.OutAtSourcePhoneNumber,
                                    OutAtTimeZone = punch.OutAtTimeZone,
                                    SourceForOutAt = punch.SourceForOutAt,
                                    TaskId = punch.TaskId,
                                    UserId = punch.UserId
                                };

                                // The split punches are processed
                                processed.Add(adjustedPunch);
                                processed.Add(newPunch);
                            }
                            else
                            {
                                // The punch does not need to be split, it is considered processed
                                processed.Add(punch);
                            }
                        }
                    }
                }
            }



            //foreach (var punch in processed)
            //{
            //    Trace.TraceInformation(string.Format("{0} thru {1}", punch.InAt.ToString("yyyy-MM-dd HH:mm:ss.fff"), punch.OutAt.Value.ToString("yyyy-MM-dd HH:mm:ss.fff")));
            //}

            return processed
                .OrderBy(p => p.UserId)
                .ThenBy(p => p.InAt)
                .ToList();

            //foreach (var punch in processed)
            //{
            //    Trace.TraceInformation(string.Format("{0} thru {1}", punch.InAt.ToString("yyyy-MM-dd HH:mm:ss.fff"), punch.OutAt.Value.ToString("yyyy-MM-dd HH:mm:ss.fff")));
            //}


            // Split based on time

            // Or split based on minutes

            //    Split based on minutes per day

            //    Split based on minutes total
        }
    }
}