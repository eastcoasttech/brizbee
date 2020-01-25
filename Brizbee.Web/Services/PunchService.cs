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
    }
}