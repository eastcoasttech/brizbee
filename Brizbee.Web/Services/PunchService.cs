using Brizbee.Common.Models;
using Brizbee.Web.Serialization;
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
                // Set all the payroll rates to the default for the task
                punch.PayrollRateId = punch.Task.BasePayrollRateId;
            }

            foreach (var option in options)
            {
                switch (option.Type)
                {
                    case "count":

                        if (option.CountScope == "day")
                        {
                            // Populates beyond the number of minutes per day
                            PopulateForCountOfMinutesPerDay(punches, userIds, option.CountMinute.Value, option.AlternatePayrollRateId.Value);
                        }
                        else if (option.CountScope == "total")
                        {
                            // Populates beyond the number of total minutes in the range
                            PopulateForCountOfTotalMinutes(punches, userIds, option.CountMinute.Value, option.AlternatePayrollRateId.Value);
                        }

                        break;
                    case "range":

                        if (option.RangeDirection == "before")
                        {
                            // Populates before the number of minutes in the day
                            PopulateBasedOnRangeBeforeOrAfter(punches, userIds, option.RangeMinutes.Value, option.AlternatePayrollRateId.Value, "before");
                        }
                        else if (option.RangeDirection == "after")
                        {
                            // Populates after the number of minutes in the day
                            PopulateBasedOnRangeBeforeOrAfter(punches, userIds, option.RangeMinutes.Value, option.AlternatePayrollRateId.Value, "after");
                        }

                        break;
                }
            }

            foreach (var punch in punches)
            {
                var rate = db.Rates.Find(punch.PayrollRateId);
                Trace.TraceInformation(string.Format("{0} {1} {2}", punch.InAt.ToString("yyyy-MM-dd HH:mm"), punch.Minutes, rate.Name));
            }

            // Save in a single transaction, so either all will fail or succeed
            //db.SaveChanges();
        }

        private void PopulateForCountOfMinutesPerDay(List<Punch> punches, int[] userIds, int minutesOfDay, int alternatePayrollRateId)
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
                            punch.PayrollRateId = alternatePayrollRateId;
                            db.Punches.Attach(punch);
                        }
                    }
                }
            }
        }

        private void PopulateForCountOfTotalMinutes(List<Punch> punches, int[] userIds, int totalMinutes, int alternatePayrollRateId)
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
                        punch.PayrollRateId = alternatePayrollRateId;
                        db.Punches.Attach(punch);
                    }
                }
            }
        }

        private void PopulateBasedOnRangeBeforeOrAfter(List<Punch> punches, int[] userIds, int minuteOfDay, int alternatePayrollRateId, string beforeOrafter = "before")
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
                                punch.PayrollRateId = alternatePayrollRateId;
                                db.Punches.Attach(punch);
                            }
                            break;
                        case "after":
                            if (minuteOfPunch >= minuteOfDay)
                            {
                                punch.PayrollRateId = alternatePayrollRateId;
                                db.Punches.Attach(punch);
                            }
                            break;
                    }
                }
            }
        }
    }
}