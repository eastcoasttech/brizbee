using Brizbee.Common.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Brizbee.Web.Services
{
    public class PunchSplitter
    {
        // 40 hours as minutes = 2400
        public void SplitAtMinutes(int[] userIds, DateTime inAt, DateTime outAt, int minuteToSplit, User currentUser)
        {
            using (var db = new SqlContext())
            {
                var organization = db.Organizations.Find(currentUser.OrganizationId);

                using (var transaction = db.Database.BeginTransaction())
                {
                    try
                    {
                        var users = db.Users.Where(u => userIds.Contains(u.Id))
                            .OrderBy(u => u.Id);
                        var finished = new List<int>();

                        foreach (var user in users)
                        {
                            double zero = 0;
                            var minutes = zero;

                            var punches = db.Punches.Where(p => p.OutAt != null) // where punch is complete
                                .Where(p => p.InAt >= inAt) // where InAt is after or at start
                                .Where(p => p.OutAt <= outAt) // where OutAt is before or at finish
                                .Where(p => p.UserId == user.Id)
                                .OrderBy(p => p.InAt);

                            foreach (var punch in punches)
                            {
                                double spanMinutes = (punch.OutAt.Value - punch.InAt).TotalMinutes;
                                
                                // Once we reach the minutes at which to split
                                // (ex. 2400 minutes would be 40 hours), then
                                // split the punch and move to the next user
                                if ((minutes + spanMinutes) == minuteToSplit)
                                {
                                    finished.Add(punch.UserId); // mark overtime for user as complete
                                    continue;
                                }
                                else if ((minutes + spanMinutes) > minuteToSplit)
                                {
                                    // 2465 - 2400 = 65
                                    // 9600 - 2400 = 9360
                                    // 2467 > 2400
                                    var difference = (minutes + spanMinutes) - minuteToSplit;
                                    SplitTimeAtDifference(punch, difference);
                                    finished.Add(punch.UserId); // mark overtime for user as complete
                                    continue;
                                }
                                else
                                {
                                    //Debug.WriteLine("Span added to total is not greater than 40 hours, 2400 minutes");
                                    minutes = minutes + spanMinutes;
                                }
                            }
                            
                            db.SaveChanges();
                        }

                        transaction.Commit();
                    }
                    catch (Exception)
                    {
                        transaction.Rollback();
                    }
                }
            }
        }
        
        // 7:00 AM = 7, 5:00 PM = 17
        public void SplitAtHour(int[] userIds, DateTime inAt, DateTime outAt, int hour, User currentUser)
        {
            using (var db = new SqlContext())
            {
                var organization = db.Organizations.Find(currentUser.OrganizationId);

                using (var transaction = db.Database.BeginTransaction())
                {
                    try
                    {
                        var punches = db.Punches
                            .Where(p => p.OutAt != null) // where punch is complete
                            .Where(p => !p.CommitId.HasValue) // only punches that have not been committed
                            .Where(p => p.InAt >= inAt) // where InAt is after or at start
                            .Where(p => p.OutAt <= outAt) // where OutAt is before or at finish
                            .Where(p => userIds.Contains(p.UserId))
                            .OrderBy(p => p.InAt);
                        
                        foreach (var punch in punches)
                        {
                            if (punch.CommitId != null)
                            {
                                throw new Exception("Cannot split a punch that has been committed");
                            }

                            var splitter = new HourSplitter(punch.InAt, punch.OutAt.Value, hour);

                            // The old punch will be deleted from the
                            // database and replaced with each new punch
                            // that was created by splitting the original one
                            db.Punches.Remove(punch);

                            foreach (Tuple<DateTime, DateTime> tuple in splitter.Punches)
                            {
                                db.Punches.Add(new Punch()
                                {
                                    CreatedAt = punch.CreatedAt,
                                    Guid = Guid.NewGuid(),
                                    InAt = tuple.Item1,
                                    OutAt = tuple.Item2,
                                    SourceForInAt = punch.SourceForInAt,
                                    SourceForOutAt = punch.SourceForOutAt,
                                    TaskId = punch.TaskId,
                                    UserId = punch.UserId
                                });
                            }
                        }
                        
                        db.SaveChanges();

                        transaction.Commit();
                    }
                    catch (Exception ex)
                    {
                        Trace.TraceWarning(ex.ToString());
                        transaction.Rollback();
                    }
                }
            }
        }
        
        /// <summary>
        /// Creates two Time entries from a single Time entry split
        /// at the given number of minutes from the "in at" DateTime.
        /// 
        /// Deletes the original time entry and creates two new ones.
        /// </summary>
        /// <param name="originalTime"></param>
        /// <param name="difference"></param>
        public void SplitTimeAtDifference(Punch originalTime, double difference)
        {
            using (var db = new SqlContext())
            {
                Trace.TraceInformation(string.Format("{0} through {1} at {2} minutes", originalTime.InAt, originalTime.OutAt, difference));
                Punch time1 = new Punch()
                {
                    InAt = originalTime.InAt,
                    OutAt = originalTime.OutAt.Value.AddMinutes(-difference), // originalTime.InAt.AddMinutes(difference)
                    TaskId = originalTime.TaskId,
                    UserId = originalTime.UserId
                };
                Punch time2 = new Punch()
                {
                    InAt = originalTime.OutAt.Value.AddMinutes(-difference),
                    OutAt = originalTime.OutAt,
                    TaskId = originalTime.TaskId,
                    UserId = originalTime.UserId
                };
                db.Punches.Remove(originalTime);
                db.Punches.Add(time1);
                db.Punches.Add(time2);

                db.SaveChanges();
            }
        }
    }
}