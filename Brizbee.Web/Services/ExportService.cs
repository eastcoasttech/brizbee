using Brizbee.Common.Database;
using Brizbee.Common.Exceptions;
using Brizbee.Common.Models;
using CsvHelper;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;

namespace Brizbee.Web.Services
{
    public class ExportService
    {
        private int? _commitId;
        private DateTime? _inAt;
        private DateTime? _outAt;

        public ExportService(int? commitId)
        {
            _commitId = commitId;
        }

        public ExportService(DateTime? inAt, DateTime? outAt)
        {
            _inAt = inAt;
            _outAt = outAt;
        }

        public string BuildCsv(string delimiter = ",")
        {
            using (var db = new SqlContext())
            {
                IQueryable<Punch> punches = db.Punches
                    .Include("Task")
                    .Include("Task.Job")
                    .Include("Task.Job.Customer")
                    .Include("User");

                if (_commitId.HasValue)
                {
                    punches = punches
                        .Where(p => p.CommitId == _commitId);
                }
                else if (_inAt.HasValue && _outAt.HasValue)
                {
                    punches = punches
                        .Where(p => p.InAt >= _inAt && p.OutAt.HasValue && p.OutAt.Value <= _outAt);
                }
                else
                {
                    throw new NotAuthorizedException("Must specify a date range or commit id to export punches.");
                }

                var list = punches
                    .OrderBy(p => p.InAt)
                    .Select(p => new
                    {
                        PunchId = p.Id,
                        PunchInAt = p.InAt,
                        PunchInAtTimeZone = p.InAtTimeZone,
                        PunchSourceForInAt = p.SourceForInAt,
                        PunchOutAt = p.OutAt,
                        PunchOutAtTimeZone = p.OutAtTimeZone,
                        PunchSourceForOutAt = p.SourceForOutAt,
                        PunchCreatedAt = p.CreatedAt,
                        User = new
                        {
                            UserId = p.User.Id,
                            UserName = p.User.Name
                        },
                        Task = new
                        {
                            TaskId = p.Task.Id,
                            TaskNumber = p.Task.Number,
                            TaskName = p.Task.Name
                        },
                        Job = new
                        {
                            JobId = p.Task.Job.Id,
                            JobNumber = p.Task.Job.Number,
                            JobName = p.Task.Job.Name
                        },
                        Customer = new
                        {
                            CustomerId = p.Task.Job.Customer.Id,
                            CustomerNumber = p.Task.Job.Customer.Number,
                            CustomerName = p.Task.Job.Customer.Name
                        },
                        PayrollRate = p.PayrollRate.Name,
                        ServiceRate = p.ServiceRate.Name
                    })
                    .ToList();

                using (var writer = new StringWriter())
                using (var csv = new CsvWriter(writer))
                {
                    csv.Configuration.Delimiter = delimiter;
                    csv.WriteRecords(list);
                    return writer.ToString();
                }
            }
        }
    }
}