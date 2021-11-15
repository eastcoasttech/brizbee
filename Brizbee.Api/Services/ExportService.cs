//
//  ExportService.cs
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

using Brizbee.Core.Exceptions;
using Brizbee.Core.Models;
using CsvHelper;
using CsvHelper.Configuration;
using Microsoft.EntityFrameworkCore;
using System.Globalization;

namespace Brizbee.Api.Services
{
    public class ExportService
    {
        private readonly SqlContext _context;
        private int? _commitId;
        private DateTime? _inAt;
        private DateTime? _outAt;
        private int _currentUserId;

        public ExportService(int? commitId, int currentUserId, SqlContext context)
        {
            _commitId = commitId;
            _context = context;
        }

        public ExportService(DateTime? inAt, DateTime? outAt, int currentUserId, SqlContext context)
        {
            _inAt = inAt;
            _outAt = outAt;
            _currentUserId = currentUserId;
            _context = context;
        }

        public string BuildCsv(string delimiter = ",")
        {
            var currentUser = _context.Users.Find(_currentUserId);

            IQueryable<Punch> punches = _context.Punches
                .Include("Task")
                .Include("Task.Job")
                .Include("Task.Job.Customer")
                .Include("User")
                .Where(p => p.Task.Job.Customer.OrganizationId == currentUser.OrganizationId);

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
                    InAtSourceHardware = p.InAtSourceHardware,
                    InAtSourcePhoneNumber = p.InAtSourcePhoneNumber,
                    InAtSourceHostname = p.InAtSourceHostname,
                    PunchOutAt = p.OutAt,
                    PunchOutAtTimeZone = p.OutAtTimeZone,
                    OutAtSourceHardware = p.OutAtSourceHardware,
                    OutAtSourcePhoneNumber = p.OutAtSourcePhoneNumber,
                    OutAtSourceHostname = p.OutAtSourceHostname,
                    PunchCreatedAtUtc = p.CreatedAt,
                    User = new
                    {
                        UserName = p.User.Name
                    },
                    Task = new
                    {
                        TaskNumber = p.Task.Number,
                        TaskName = p.Task.Name
                    },
                    Job = new
                    {
                        JobNumber = p.Task.Job.Number,
                        JobName = p.Task.Job.Name
                    },
                    Customer = new
                    {
                        CustomerNumber = p.Task.Job.Customer.Number,
                        CustomerName = p.Task.Job.Customer.Name
                    },
                    PayrollRate = p.PayrollRate.Name,
                    ServiceRate = p.ServiceRate.Name,
                    Locked = p.CommitId.HasValue ? "X" : "",
                    LockId = p.CommitId.HasValue ? p.CommitId.ToString() : ""
                })
                .ToList();

            var configuration = new CsvConfiguration(CultureInfo.CurrentCulture)
            {
                Delimiter = delimiter
            };
            using (var writer = new StringWriter())
            using (var csv = new CsvWriter(writer, configuration))
            {
                csv.WriteRecords(list);
                return writer.ToString();
            }
        }
    }
}