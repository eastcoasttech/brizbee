//
//  PunchRepository.cs
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
using Dapper;
using Microsoft.ApplicationInsights;
using Microsoft.AspNet.OData;
using Newtonsoft.Json;
using NodaTime;
using System;
using System.Configuration;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Linq;

namespace Brizbee.Web.Repositories
{
    public class PunchRepository : IDisposable
    {
        private SqlContext db = new SqlContext();
        private TelemetryClient telemetryClient = new TelemetryClient();

        /// <summary>
        /// Disposes of the database connection.
        /// </summary>
        public void Dispose()
        {
            db.Dispose();
        }

        /// <summary>
        /// Creates a punch in the database for the given user with the
        /// given task and current timestamp.
        /// </summary>
        /// <param name="taskId">The id of the task</param>
        /// <param name="currentUser">The user to punch in</param>
        public Punch PunchIn(
            int taskId,
            User currentUser,
            string source,
            string timezone,
            string latitude = null,
            string longitude = null,
            string sourceHardware = "",
            string sourceHostname = "",
            string sourceIpAddress = "",
            string sourceOperatingSystem = "",
            string sourceOperatingSystemVersion = "",
            string sourceBrowser = "",
            string sourceBrowserVersion = "",
            string sourcePhoneNumber = "")
        {
            var punch = new Punch();
            var tz = DateTimeZoneProviders.Tzdb.GetZoneOrNull(timezone);
            var nowInstant = SystemClock.Instance.GetCurrentInstant();
            var nowLocal = nowInstant.InZone(tz);
            var nowDateTime = nowLocal.LocalDateTime.ToDateTimeUnspecified();
            var zero = new DateTime(
                nowDateTime.Year,
                nowDateTime.Month,
                nowDateTime.Day,
                nowDateTime.Hour,
                nowDateTime.Minute,
                0,
                0);

            var existing = db.Punches.Where(p => p.UserId == currentUser.Id)
                .Where(p => p.OutAt == null)
                .OrderByDescending(p => p.InAt)
                .FirstOrDefault();

            if (existing != null)
            {
                // Record the object before any changes are made.
                var before = JsonConvert.SerializeObject(existing);

                // Punch out the user
                punch.OutAtSourceHardware = sourceHardware;
                punch.OutAtSourceHostname = sourceHostname;
                punch.OutAtSourceIpAddress = sourceIpAddress;
                punch.OutAtSourceOperatingSystem = sourceOperatingSystem;
                punch.OutAtSourceOperatingSystemVersion = sourceOperatingSystemVersion;
                punch.OutAtSourceBrowser = sourceBrowser;
                punch.OutAtSourceBrowserVersion = sourceBrowserVersion;
                punch.OutAtSourcePhoneNumber = sourcePhoneNumber;
                punch.InAt = zero;
                existing.OutAt = zero;
                existing.OutAtTimeZone = timezone;
                existing.LatitudeForOutAt = latitude;
                existing.LongitudeForOutAt = longitude;

                // Record the activity.
                AuditPunch(existing.Id, before, JsonConvert.SerializeObject(existing), currentUser, "UPDATE");
            }
            else
            {
                punch.InAt = zero;
            }

            // Auto-generated
            punch.InAtSourceHardware = sourceHardware;
            punch.InAtSourceHostname = sourceHostname;
            punch.InAtSourceIpAddress = sourceIpAddress;
            punch.InAtSourceOperatingSystem = sourceOperatingSystem;
            punch.InAtSourceOperatingSystemVersion = sourceOperatingSystemVersion;
            punch.InAtSourceBrowser = sourceBrowser;
            punch.InAtSourceBrowserVersion = sourceBrowserVersion;
            punch.InAtSourcePhoneNumber = sourcePhoneNumber;
            punch.CreatedAt = nowInstant.InUtc().ToDateTimeUtc();
            punch.TaskId = taskId;
            punch.UserId = currentUser.Id;
            punch.Guid = Guid.NewGuid();
            punch.SourceForInAt = source;
            punch.InAtTimeZone = timezone;
            punch.LatitudeForInAt = latitude;
            punch.LongitudeForInAt = longitude;

            db.Punches.Add(punch);

            db.SaveChanges();

            // Record the activity.
            AuditPunch(punch.Id, null, JsonConvert.SerializeObject(punch), currentUser, "CREATE");

            return punch;
        }

        /// <summary>
        /// Finds the most recent punch in the database which does not
        /// have an "out" timestamp, and updates the punch's out value
        /// to be the current timestamp.
        /// </summary>
        /// <param name="currentUser">The user to punch out</param>
        public Punch PunchOut(
            User currentUser,
            string source,
            string timezone,
            string latitude = null,
            string longitude = null,
            string sourceHardware = "",
            string sourceHostname = "",
            string sourceIpAddress = "",
            string sourceOperatingSystem = "",
            string sourceOperatingSystemVersion = "",
            string sourceBrowser = "",
            string sourceBrowserVersion = "",
            string sourcePhoneNumber = "")
        {
            var punch = db.Punches
                .Where(p => p.UserId == currentUser.Id)
                .Where(p => !p.OutAt.HasValue)
                .OrderByDescending(p => p.InAt)
                .FirstOrDefault();

            // Record the object before any changes are made.
            var before = JsonConvert.SerializeObject(punch);

            var tz = DateTimeZoneProviders.Tzdb.GetZoneOrNull(timezone);
            var nowInstant = SystemClock.Instance.GetCurrentInstant();
            var nowLocal = nowInstant.InZone(tz);
            var nowDateTime = nowLocal.LocalDateTime.ToDateTimeUnspecified();
            var zero = new DateTime(
                nowDateTime.Year,
                nowDateTime.Month,
                nowDateTime.Day,
                nowDateTime.Hour,
                nowDateTime.Minute,
                0,
                0);

            punch.OutAtSourceHardware = sourceHardware;
            punch.OutAtSourceHostname = sourceHostname;
            punch.OutAtSourceIpAddress = sourceIpAddress;
            punch.OutAtSourceOperatingSystem = sourceOperatingSystem;
            punch.OutAtSourceOperatingSystemVersion = sourceOperatingSystemVersion;
            punch.OutAtSourceBrowser = sourceBrowser;
            punch.OutAtSourceBrowserVersion = sourceBrowserVersion;
            punch.OutAtSourcePhoneNumber = sourcePhoneNumber;
            punch.OutAt = zero;
            punch.SourceForOutAt = source;
            punch.OutAtTimeZone = timezone;
            punch.LatitudeForOutAt = latitude;
            punch.LongitudeForOutAt = longitude;

            db.SaveChanges();

            // Record the activity.
            AuditPunch(punch.Id, before, JsonConvert.SerializeObject(punch), currentUser, "UPDATE");

            return punch;
        }

        private void AuditPunch(int id, string before, string after, User currentUser, string action)
        {
            try
            {
                using (var connection = new SqlConnection(ConfigurationManager.ConnectionStrings["SqlContext"].ToString()))
                {
                    string insertQuery = @"
                        INSERT INTO [PunchAudits]
                            ([CreatedAt], [ObjectId], [OrganizationId], [UserId], [Action], [Before], [After])
                        VALUES
                            (@CreatedAt, @ObjectId, @OrganizationId, @UserId, @Action, @Before, @After);";

                    var result = connection.Execute(insertQuery, new
                    {
                        CreatedAt = DateTime.UtcNow,
                        ObjectId = id,
                        OrganizationId = currentUser.OrganizationId,
                        UserId = currentUser.Id,
                        Action = action,
                        Before = before,
                        After = after
                    });
                }
            }
            catch (Exception ex)
            {
                telemetryClient.TrackException(ex);
            }
        }
    }
}