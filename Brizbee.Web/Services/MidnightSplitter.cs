//
//  MidnightSplitter.cs
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
                var adjustedOutAt = new DateTime(originalInAt.Year, originalInAt.Month, originalInAt.Day, 0, 0, 0, 0).AddDays(1); // Midnight on next day

                var newInAt = new DateTime(originalInAt.Year, originalInAt.Month, originalInAt.Day, 0, 0, 0, 0).AddDays(1); // Midnight on next day
                var newOutAt = originalOutAt; // End of range, not safe, could extend past midnight

                // Extract information from the original punch into two new split punches

                var adjustedPunch = new Punch() // Processed
                {
                    InAt = adjustedInAt,
                    OutAt = adjustedOutAt,
                    Guid = Guid.NewGuid(),
                    CreatedAt = DateTime.UtcNow,
                    LatitudeForInAt = originalPunch.LatitudeForInAt,
                    LatitudeForOutAt = originalPunch.LatitudeForOutAt,
                    LongitudeForInAt = originalPunch.LongitudeForInAt,
                    LongitudeForOutAt = originalPunch.LongitudeForOutAt,
                    InAtSourceBrowser = originalPunch.InAtSourceBrowser,
                    InAtSourceBrowserVersion = originalPunch.InAtSourceBrowserVersion,
                    InAtSourceHardware = originalPunch.InAtSourceHardware,
                    InAtSourceHostname = originalPunch.InAtSourceHostname,
                    InAtSourceIpAddress = originalPunch.InAtSourceIpAddress,
                    InAtSourceOperatingSystem = originalPunch.InAtSourceOperatingSystem,
                    InAtSourceOperatingSystemVersion = originalPunch.InAtSourceOperatingSystemVersion,
                    InAtSourcePhoneNumber = originalPunch.InAtSourcePhoneNumber,
                    InAtTimeZone = originalPunch.InAtTimeZone,
                    SourceForInAt = originalPunch.SourceForInAt,
                    OutAtSourceBrowser = originalPunch.OutAtSourceBrowser,
                    OutAtSourceBrowserVersion = originalPunch.OutAtSourceBrowserVersion,
                    OutAtSourceHardware = originalPunch.OutAtSourceHardware,
                    OutAtSourceHostname = originalPunch.OutAtSourceHostname,
                    OutAtSourceIpAddress = originalPunch.OutAtSourceIpAddress,
                    OutAtSourceOperatingSystem = originalPunch.OutAtSourceOperatingSystem,
                    OutAtSourceOperatingSystemVersion = originalPunch.OutAtSourceOperatingSystemVersion,
                    OutAtSourcePhoneNumber = originalPunch.OutAtSourcePhoneNumber,
                    OutAtTimeZone = originalPunch.OutAtTimeZone,
                    SourceForOutAt = originalPunch.SourceForOutAt,
                    TaskId = originalPunch.TaskId,
                    UserId = originalPunch.UserId
                };
                var newPunch = new Punch() // Not safe, must be checked again
                {
                    InAt = newInAt,
                    OutAt = newOutAt,
                    Guid = Guid.NewGuid(),
                    CreatedAt = DateTime.UtcNow,
                    LatitudeForInAt = originalPunch.LatitudeForInAt,
                    LatitudeForOutAt = originalPunch.LatitudeForOutAt,
                    LongitudeForInAt = originalPunch.LongitudeForInAt,
                    LongitudeForOutAt = originalPunch.LongitudeForOutAt,
                    InAtSourceBrowser = originalPunch.InAtSourceBrowser,
                    InAtSourceBrowserVersion = originalPunch.InAtSourceBrowserVersion,
                    InAtSourceHardware = originalPunch.InAtSourceHardware,
                    InAtSourceHostname = originalPunch.InAtSourceHostname,
                    InAtSourceIpAddress = originalPunch.InAtSourceIpAddress,
                    InAtSourceOperatingSystem = originalPunch.InAtSourceOperatingSystem,
                    InAtSourceOperatingSystemVersion = originalPunch.InAtSourceOperatingSystemVersion,
                    InAtSourcePhoneNumber = originalPunch.InAtSourcePhoneNumber,
                    InAtTimeZone = originalPunch.InAtTimeZone,
                    SourceForInAt = originalPunch.SourceForInAt,
                    OutAtSourceBrowser = originalPunch.OutAtSourceBrowser,
                    OutAtSourceBrowserVersion = originalPunch.OutAtSourceBrowserVersion,
                    OutAtSourceHardware = originalPunch.OutAtSourceHardware,
                    OutAtSourceHostname = originalPunch.OutAtSourceHostname,
                    OutAtSourceIpAddress = originalPunch.OutAtSourceIpAddress,
                    OutAtSourceOperatingSystem = originalPunch.OutAtSourceOperatingSystem,
                    OutAtSourceOperatingSystemVersion = originalPunch.OutAtSourceOperatingSystemVersion,
                    OutAtSourcePhoneNumber = originalPunch.OutAtSourcePhoneNumber,
                    OutAtTimeZone = originalPunch.OutAtTimeZone,
                    SourceForOutAt = originalPunch.SourceForOutAt,
                    TaskId = originalPunch.TaskId,
                    UserId = originalPunch.UserId
                };

                // The adjusted punch is now processed
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