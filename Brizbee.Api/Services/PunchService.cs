﻿//
//  PunchService.cs
//  BRIZBEE API
//
//  Copyright (C) 2019-2021 East Coast Technology Services, LLC
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

using Brizbee.Api.Serialization;
using Brizbee.Core.Models;

namespace Brizbee.Api.Services
{
    public class PunchService
    {
        private readonly SqlContext _context;
        private DateTime nowUtc = DateTime.UtcNow;

        public PunchService(SqlContext context)
        {
            _context = context;
        }

        public List<Punch> Populate(PopulateRateOptions populateOptions, List<Punch> originalPunches, User currentUser)
        {
            // Clear id to zero
            foreach (var punch in originalPunches)
            {
                punch.Id = 0;
            }

            var inAt = populateOptions.InAt;
            var outAt = populateOptions.OutAt;
            var options = populateOptions.Options.ToList().OrderBy(o => o.Order);
            var userIds = originalPunches
                .GroupBy(p => p.UserId)
                .Select(g => g.Key)
                .ToArray();

            // Split at midnight
            var splitPunches = SplitAtMidnight(originalPunches, currentUser);

            // Then split at each additional interval
            foreach (var option in options)
            {
                switch (option.Type)
                {
                    case "count":

                        var minute = option.CountMinute.Value;
                        
                        if (option.CountScope == "day")
                        {
                            splitPunches = SplitAtMinute(splitPunches, currentUser, minutesPerDay: minute);
                        }
                        else if (option.CountScope == "total")
                        {
                            splitPunches = SplitAtMinute(splitPunches, currentUser, minutesPerSpan: minute);
                        }

                        break;
                    case "range":

                        var minutes = option.RangeMinutes.Value;
                        splitPunches = SplitAtMinute(splitPunches, currentUser, minuteOfDay: minutes);

                        break;
                }
            }

            // Populate the base rate before any of the alternate rates are populated
            foreach (var punch in splitPunches)
            {
                var task = _context.Tasks.Find(punch.TaskId);
                punch.PayrollRateId = task.BasePayrollRateId;
                punch.ServiceRateId = task.BaseServiceRateId;
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

                        var minute = option.CountMinute.Value;

                        if (option.CountScope == "day")
                        {
                            // Populates beyond the number of minutes per day
                            PopulateForCountOfMinutesPerDay(
                                splitPunches,
                                userIds,
                                minute,
                                baseRateId,
                                alternateRateId,
                                rate);
                        }
                        else if (option.CountScope == "total")
                        {
                            // Populates beyond the number of total minutes in the range
                            PopulateForCountOfTotalMinutes(
                                splitPunches,
                                userIds,
                                minute,
                                baseRateId,
                                alternateRateId,
                                rate);
                        }

                        break;
                    case "range":

                        var minutes = option.RangeMinutes.Value;

                        if (option.RangeDirection == "before")
                        {
                            // Populates before the number of minutes in the day
                            PopulateBasedOnRangeBeforeOrAfter(
                                splitPunches,
                                userIds,
                                minutes,
                                baseRateId,
                                alternateRateId,
                                "before",
                                rate);
                        }
                        else if (option.RangeDirection == "after")
                        {
                            // Populates after the number of minutes in the day
                            PopulateBasedOnRangeBeforeOrAfter(
                                splitPunches,
                                userIds,
                                minutes,
                                baseRateId,
                                alternateRateId,
                                "after",
                                rate);
                        }

                        break;
                    case "date":

                        // Populates for a specific date
                        PopulateForDay(
                            splitPunches,
                            userIds,
                            option.Date,
                            baseRateId,
                            alternateRateId,
                            rate);

                        break;
                    case "dayofweek":

                        // Populates for day of week
                        PopulateForDayOfWeek(
                            splitPunches,
                            userIds,
                            option.DayOfWeek,
                            baseRateId,
                            alternateRateId,
                            rate);

                        break;
                }
            }

            var ordered = splitPunches.OrderBy(p => p.UserId).ThenBy(p => p.InAt).ToList();

            foreach (var punch in ordered)
            {
                var payrollRate = _context.Rates.Find(punch.PayrollRateId);
                var serviceRate = _context.Rates.Find(punch.ServiceRateId);
            }

            return ordered;
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
                    long count = 0;
                    foreach (var punch in punchesForDay)
                    {
                        var task = _context.Tasks.Find(punch.TaskId);

                        count += punch.Minutes;

                        if (count > minutesOfDay)
                        {
                            if (rate == "payroll")
                            {
                                // Can only set alternate rates for the matching base rate
                                if (task.BasePayrollRateId == baseRateId)
                                {
                                    punch.PayrollRateId = alternateRateId;
                                }
                            }
                            else if (rate == "service")
                            {
                                // Can only set alternate rates for the matching base rate
                                if (task.BaseServiceRateId == baseRateId)
                                {
                                    punch.ServiceRateId = alternateRateId;
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
                long count = 0;
                foreach (var punch in filtered)
                {
                    var task = _context.Tasks.Find(punch.TaskId);

                    count += punch.Minutes;

                    if (count > totalMinutes)
                    {
                        if (rate == "payroll")
                        {
                            // Can only set alternate rates for the matching base rate
                            if (task.BasePayrollRateId == baseRateId)
                            {
                                punch.PayrollRateId = alternateRateId;
                            }
                        }
                        else if (rate == "service")
                        {
                            // Can only set alternate rates for the matching base rate
                            if (task.BaseServiceRateId == baseRateId)
                            {
                                punch.ServiceRateId = alternateRateId;
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
                    var task = _context.Tasks.Find(punch.TaskId);

                    var minuteOfPunch = (punch.InAt.Hour * 60) + punch.InAt.Minute;
                    switch (beforeOrafter)
                    {
                        case "before":
                            if (minuteOfPunch < minuteOfDay)
                            {
                                if (rate == "payroll")
                                {
                                    // Can only set alternate rates for the matching base rate
                                    if (task.BasePayrollRateId == baseRateId)
                                    {
                                        punch.PayrollRateId = alternateRateId;
                                    }
                                }
                                else if (rate == "service")
                                {
                                    // Can only set alternate rates for the matching base rate
                                    if (task.BaseServiceRateId == baseRateId)
                                    {
                                        punch.ServiceRateId = alternateRateId;
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
                                    if (task.BasePayrollRateId == baseRateId)
                                    {
                                        punch.PayrollRateId = alternateRateId;
                                    }
                                }
                                else if (rate == "service")
                                {
                                    // Can only set alternate rates for the matching base rate
                                    if (task.BaseServiceRateId == baseRateId)
                                    {
                                        punch.ServiceRateId = alternateRateId;
                                    }
                                }
                            }
                            break;
                    }
                }
            }
        }
    
        private void PopulateForDay(List<Punch> punches, int[] userIds, DateTime date, int baseRateId, int alternateRateId, string rate = "payroll")
        {
            foreach (var punch in punches)
            {
                if (punch.InAt.Date == date.Date)
                {
                    var task = _context.Tasks.Find(punch.TaskId);

                    if (rate == "payroll")
                    {
                        // Can only set alternate rates for the matching base rate
                        if (task.BasePayrollRateId == baseRateId)
                        {
                            punch.PayrollRateId = alternateRateId;
                        }
                    }
                    else if (rate == "service")
                    {
                        // Can only set alternate rates for the matching base rate
                        if (task.BaseServiceRateId == baseRateId)
                        {
                            punch.ServiceRateId = alternateRateId;
                        }
                    }
                }
            }
        }

        private void PopulateForDayOfWeek(List<Punch> punches, int[] userIds, string dayOfWeek, int baseRateId, int alternateRateId, string rate = "payroll")
        {
            DayOfWeek? day = null;
            switch (dayOfWeek)
            {
                case "Sunday":
                    day = DayOfWeek.Sunday;
                    break;
                case "Monday":
                    day = DayOfWeek.Monday;
                    break;
                case "Tuesday":
                    day = DayOfWeek.Tuesday;
                    break;
                case "Wednesday":
                    day = DayOfWeek.Wednesday;
                    break;
                case "Thursday":
                    day = DayOfWeek.Thursday;
                    break;
                case "Friday":
                    day = DayOfWeek.Friday;
                    break;
                case "Saturday":
                    day = DayOfWeek.Saturday;
                    break;
            }

            if (day == null)
                return;

            var filtered = punches.Where(p => p.InAt.DayOfWeek == day).ToList();
            foreach (var punch in filtered)
            {
                var task = _context.Tasks.Find(punch.TaskId);

                if (rate == "payroll")
                {
                    // Can only set alternate rates for the matching base rate
                    if (task.BasePayrollRateId == baseRateId)
                    {
                        punch.PayrollRateId = alternateRateId;
                    }
                }
                else if (rate == "service")
                {
                    // Can only set alternate rates for the matching base rate
                    if (task.BaseServiceRateId == baseRateId)
                    {
                        punch.ServiceRateId = alternateRateId;
                    }
                }
            }
        }

        public List<Punch> SplitAtMidnight(List<Punch> originalPunches, User currentUser)
        {
            // Clear id to zero
            foreach (var punch in originalPunches)
            {
                punch.Id = 0;
            }

            var processed = new List<Punch>(0);
            var splitter = new MidnightSplitter();

            foreach (var punch in originalPunches)
            {
                processed.AddRange(splitter.Split(punch));
            }

            // Order the processed punches
            processed = processed
                .OrderBy(p => p.UserId)
                .ThenBy(p => p.InAt)
                .ToList();

            return processed;
        }

        public List<Punch> SplitAtMinute(List<Punch> punches, User currentUser, int? minuteOfDay = null, int? minutesPerDay = null, int? minutesPerSpan = null)
        {
            var processed = new List<Punch>();

            var userIds = punches
                .GroupBy(p => p.UserId)
                .Select(g => g.Key)
                .ToArray();

            // Must perform split for each user independently
            foreach (var userId in userIds)
            {
                // Order the user's punches
                var filtered = punches
                    .Where(p => p.UserId == userId)
                    .OrderBy(p => p.InAt)
                    .ToList();

                // Split at specific time of day
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

                            var minuteOfInAt = spanInAt.TotalMinutes;
                            var minuteOfOutAt = spanOutAt.TotalMinutes;

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
                                    CreatedAt = nowUtc,
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
                                    CreatedAt = nowUtc,
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

                // Split at count of minutes per day
                else if (minutesPerDay.HasValue)
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

                        long count = 0;
                        var split = false;
                        foreach (var punch in punchesForDay) // 10:15am - 4:30pm = 375 minutes
                        {
                            if (split)
                            {
                                // If punches have been split, do not further process
                                processed.Add(punch);
                                continue;
                            }

                            // Check that punch doesn't exceed minutes per day
                            if (count + punch.Minutes > minutesPerDay) // 375 > 300
                            {
                                // It does, this is when to split

                                var minutesExceeded = (count + punch.Minutes) - minutesPerDay; // 75 minutes exceeded

                                var originalInAt = punch.InAt;
                                var originalOutAt = punch.OutAt.Value;

                                var adjustedInAt = originalInAt;
                                var adjustedOutAt = punch.OutAt.Value.AddMinutes(-Convert.ToInt32(minutesExceeded));

                                var newInAt = punch.OutAt.Value.AddMinutes(-Convert.ToInt32(minutesExceeded));
                                var newOutAt = originalOutAt;

                                var adjustedPunch = new Punch()
                                {
                                    InAt = adjustedInAt,
                                    OutAt = adjustedOutAt,
                                    Guid = Guid.NewGuid(),
                                    CreatedAt = nowUtc,
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
                                    CreatedAt = nowUtc,
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

                                // Add the punches and next loop will be caught by check above
                                processed.Add(adjustedPunch);
                                processed.Add(newPunch);
                                split = true;
                            }
                            else
                            {
                                // Continue to the next one
                                count += punch.Minutes;
                                processed.Add(punch);
                            }
                        }
                    }
                }

                // Split at count of minutes total
                else if (minutesPerSpan.HasValue)
                {
                    long count = 0;
                    var split = false;
                    foreach (var punch in filtered) // 10:15am - 4:30pm = 375 minutes
                    {
                        if (split)
                        {
                            // If punches have been split, do not further process
                            processed.Add(punch);
                            continue;
                        }

                        // Check that punch doesn't exceed minutes per span
                        if (count + punch.Minutes > minutesPerSpan) // 375 > 300
                        {
                            // It does, this is when to split

                            var minutesExceeded = (count + punch.Minutes) - minutesPerSpan; // 75 minutes exceeded

                            var originalInAt = punch.InAt;
                            var originalOutAt = punch.OutAt.Value;

                            var adjustedInAt = originalInAt;
                            var adjustedOutAt = punch.OutAt.Value.AddMinutes(-Convert.ToInt32(minutesExceeded));

                            var newInAt = punch.OutAt.Value.AddMinutes(-Convert.ToInt32(minutesExceeded));
                            var newOutAt = originalOutAt;

                            if (adjustedOutAt.Subtract(adjustedInAt).TotalMinutes != 0)
                            {
                                var adjustedPunch = new Punch()
                                {
                                    InAt = adjustedInAt,
                                    OutAt = adjustedOutAt,
                                    Guid = Guid.NewGuid(),
                                    CreatedAt = nowUtc,
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

                                // Add the punch and the next loop will be caught by check above
                                processed.Add(adjustedPunch);
                            }

                            if (newOutAt.Subtract(newInAt).TotalMinutes != 0)
                            {
                                var newPunch = new Punch()
                                {
                                    InAt = newInAt,
                                    OutAt = newOutAt,
                                    Guid = Guid.NewGuid(),
                                    CreatedAt = nowUtc,
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

                                // Add the punch and the next loop will be caught by check above
                                processed.Add(newPunch);
                            }

                            split = true;
                        }
                        else
                        {
                            // Continue to the next one
                            count += punch.Minutes;
                            processed.Add(punch);
                        }
                    }
                }
            }

            // Order the processed punches
            processed = processed
                .OrderBy(p => p.UserId)
                .ThenBy(p => p.InAt)
                .ToList();

            return processed;
        }
    }
}