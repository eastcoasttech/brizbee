using Brizbee.Common.Models;
using Brizbee.Web.Policies;
using Microsoft.AspNet.OData;
using NodaTime;
using System;
using System.Diagnostics;
using System.Linq;

namespace Brizbee.Web.Repositories
{
    public class PunchRepository : IDisposable
    {
        private BrizbeeWebContext db = new BrizbeeWebContext();

        /// <summary>
        /// Creates the given punch in the database.
        /// </summary>
        /// <param name="punch">The punch to create</param>
        /// <param name="currentUser">The user to check for permissions</param>
        /// <returns>The created punch</returns>
        public Punch Create(Punch punch, User currentUser)
        {
            var now = DateTime.UtcNow;
            var organization = db.Organizations.Find(currentUser.OrganizationId);

            // Auto-generated
            punch.CreatedAt = now;
            punch.Guid = Guid.NewGuid();
            punch.InAt = new DateTime(punch.InAt.Year, punch.InAt.Month, punch.InAt.Day, punch.InAt.Hour, punch.InAt.Minute, 0);
            
            if (punch.OutAt.HasValue)
            {
                punch.OutAt = new DateTime(punch.OutAt.Value.Year, punch.OutAt.Value.Month, punch.OutAt.Value.Day, punch.OutAt.Value.Hour, punch.OutAt.Value.Minute, 59);
            }

            db.Punches.Add(punch);

            db.SaveChanges();

            return punch;
        }

        /// <summary>
        /// Deletes the punch with the given id.
        /// </summary>
        /// <param name="id">The id of the punch</param>
        /// <param name="currentUser">The user to check for permissions</param>
        public void Delete(int id, User currentUser)
        {
            var punch = db.Punches.Find(id);

            // Ensure that user is authorized
            if (!PunchPolicy.CanDelete(punch, currentUser))
            {
                throw new Exception("Not authorized to delete the object");
            }

            // Delete the object itself
            db.Punches.Remove(punch);

            db.SaveChanges();
        }

        /// <summary>
        /// Disposes of the database connection.
        /// </summary>
        public void Dispose()
        {
            db.Dispose();
        }

        /// <summary>
        /// Returns the punch with the given id.
        /// </summary>
        /// <param name="id">The id of the punch</param>
        /// <param name="currentUser">The user to check for permissions</param>
        /// <returns>The punch with the given id</returns>
        public IQueryable<Punch> Get(int id, User currentUser)
        {
            var userIds = db.Users
                .Where(u => u.OrganizationId == currentUser.OrganizationId)
                .Select(u => u.Id);
            return db.Punches
                .Where(p => userIds.Contains(p.UserId))
                .Where(p => p.Id == id);
        }

        /// <summary>
        /// Returns a queryable collection of punches.
        /// </summary>
        /// <param name="currentUser">The user to check for permissions</param>
        /// <returns>The queryable collection of punches</returns>
        public IQueryable<Punch> GetAll(User currentUser)
        {
            var userIds = db.Users
                .Where(u => u.OrganizationId == currentUser.OrganizationId)
                .Select(u => u.Id);
            return db.Punches.Where(p => userIds.Contains(p.UserId));
        }

        /// <summary>
        /// Updates the given punch with the given delta of changes.
        /// </summary>
        /// <param name="id">The id of the punch</param>
        /// <param name="patch">The changes that should be made to the punch</param>
        /// <param name="currentUser">The user to check for permissions</param>
        /// <returns>The updated punch</returns>
        public Punch Update(int id, Delta<Punch> patch, User currentUser)
        {
            var punch = db.Punches.Find(id);

            //// Ensure that object was found
            //ValidateFound(group, id);

            //// Ensure that user is authorized
            //if (!GroupPolicy.CanUpdate(currentUser, group))
            //{
            //    var resp = new HttpResponseMessage(HttpStatusCode.Forbidden)
            //    {
            //        Content = new StringContent("No permission to modify the group"),
            //        ReasonPhrase = "Permission Denied"
            //    };
            //    throw new HttpResponseException(resp);
            //}

            // Do not allow modifying some properties
            if (patch.GetChangedPropertyNames().Contains("OrganizationId"))
            {
                //var resp = new HttpResponseMessage(HttpStatusCode.Forbidden)
                //{
                //    Content = new StringContent("Cannot modify OrganizationId"),
                //    ReasonPhrase = "Permission Denied"
                //};
                //throw new HttpResponseException(resp);
                throw new Exception("Cannot modify OrganizationId");
            }

            // Peform the update
            patch.Patch(punch);
            
            db.SaveChanges();

            return punch;
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
            var fiftyNine = new DateTime(
                nowDateTime.Year,
                nowDateTime.Month,
                nowDateTime.Day,
                nowDateTime.Hour,
                nowDateTime.Minute,
                59);
            var zero = new DateTime(
                nowDateTime.Year,
                nowDateTime.Month,
                nowDateTime.Day,
                nowDateTime.Hour,
                nowDateTime.Minute,
                0);

            var existing = db.Punches.Where(p => p.UserId == currentUser.Id)
                .Where(p => p.OutAt == null)
                .OrderByDescending(p => p.InAt)
                .FirstOrDefault();

            if (existing != null)
            {
                // Punch out the user
                punch.OutAtSourceHardware = sourceHardware;
                punch.OutAtSourceHostname = sourceHostname;
                punch.OutAtSourceIpAddress = sourceIpAddress;
                punch.OutAtSourceOperatingSystem = sourceOperatingSystem;
                punch.OutAtSourceOperatingSystemVersion = sourceOperatingSystemVersion;
                punch.OutAtSourceBrowser = sourceBrowser;
                punch.OutAtSourceBrowserVersion = sourceBrowserVersion;
                punch.OutAtSourcePhoneNumber = sourcePhoneNumber;
                existing.OutAt = fiftyNine;
                existing.OutAtTimeZone = timezone;
                punch.InAt = zero.AddMinutes(1);
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
            var tz = DateTimeZoneProviders.Tzdb.GetZoneOrNull(timezone);
            var nowInstant = SystemClock.Instance.GetCurrentInstant();
            var nowLocal = nowInstant.InZone(tz);
            var nowDateTime = nowLocal.LocalDateTime.ToDateTimeUnspecified();
            var fiftyNine = new DateTime(
                nowDateTime.Year,
                nowDateTime.Month,
                nowDateTime.Day,
                nowDateTime.Hour,
                nowDateTime.Minute,
                59);

            punch.OutAtSourceHardware = sourceHardware;
            punch.OutAtSourceHostname = sourceHostname;
            punch.OutAtSourceIpAddress = sourceIpAddress;
            punch.OutAtSourceOperatingSystem = sourceOperatingSystem;
            punch.OutAtSourceOperatingSystemVersion = sourceOperatingSystemVersion;
            punch.OutAtSourceBrowser = sourceBrowser;
            punch.OutAtSourceBrowserVersion = sourceBrowserVersion;
            punch.OutAtSourcePhoneNumber = sourcePhoneNumber;
            punch.OutAt = fiftyNine;
            punch.SourceForOutAt = source;
            punch.OutAtTimeZone = timezone;
            punch.LatitudeForOutAt = latitude;
            punch.LongitudeForOutAt = longitude;

            db.SaveChanges();

            return punch;
        }
    }
}