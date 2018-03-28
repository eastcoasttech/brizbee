using Brizbee.Common.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Brizbee.Services
{
    public class PunchSplitter
    {
        // 40 hours as minutes = 2400
        public void SplitAtMinutes(int[] userIds, DateTime start, DateTime finish, int minute, User currentUser)
        {
            using (var db = new BrizbeeWebContext())
            {
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
                                .Where(p => p.InAt >= start) // where InAt is after or at start
                                .Where(p => p.OutAt <= finish) // where OutAt is before or at finish
                                .Where(p => p.UserId == user.Id)
                                .OrderBy(p => p.InAt);

                            foreach (var punch in punches)
                            {
                                double spanMinutes = (punch.OutAt.Value - punch.InAt).TotalMinutes;

                                // once we reach 40 hours, split the time then skip this user in the future
                                // 40 hours = 2400 mins
                                if ((minutes + spanMinutes) == 2400)
                                {
                                    finished.Add(punch.UserId); // mark overtime for user as complete
                                    continue;
                                }
                                else if ((minutes + spanMinutes) > 2400)
                                {
                                    // 2465 - 2400 = 65
                                    // 9600 - 2400 = 9360
                                    //Debug.WriteLine("Span added to total would be greater than 40 hours, 2400 minutes");
                                    // 2467 > 2400
                                    var difference = (minutes + spanMinutes) - 2400;
                                    SplitTimeAtDifference(t, difference);
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
        public void SplitAtHour(int[] userIds, DateTime start, DateTime finish, int hour, User currentUser)
        {
            using (var db = new BrizbeeWebContext())
            {
                using (var transaction = db.Database.BeginTransaction())
                {
                    try
                    {
                        var punches = db.Punches.Where(p => p.OutAt != null) // where punch is complete
                            .Where(p => p.InAt >= start) // where InAt is after or at start
                            .Where(p => p.OutAt <= finish) // where OutAt is before or at finish
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

                            foreach (Tuple<DateTime, DateTime> tuple in splitter.Results)
                            {
                                db.Punches.Add(new Punch()
                                {
                                    CreatedAt = punch.CreatedAt,
                                    Guid = Guid.NewGuid(),
                                    InAt = tuple.Item1,
                                    OutAt = tuple.Item2,
                                    TaskId = punch.TaskId,
                                    UserId = punch.UserId
                                });
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
    }
}