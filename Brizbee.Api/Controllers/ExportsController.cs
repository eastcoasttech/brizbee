//
//  ExportsController.cs
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

using Brizbee.Api.Services;
using Brizbee.Core.Exceptions;
using Brizbee.Core.Models;
using Microsoft.AspNetCore.Mvc;
using System.Text;

namespace Brizbee.Api.Controllers
{
    public class ExportsController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        private readonly SqlContext _context;

        public ExportsController(IConfiguration configuration, SqlContext context)
        {
            _configuration = configuration;
            _context = context;
        }

        // GET: api/Exports/Csv
        [HttpGet("api/Exports/Csv")]
        public IActionResult GetCsv(
            [FromQuery] string Delimiter,
            [FromQuery] int? CommitId = null,
            [FromQuery] DateTime? InAt = null,
            [FromQuery] DateTime? OutAt = null)
        {
            var currentUser = CurrentUser();

            if (CommitId.HasValue)
            {
                var commit = _context.Commits.Find(CommitId.Value);
                var exportService = new ExportService(commit.Id, currentUser.Id, _context);

                string csv = exportService.BuildCsv(Delimiter);
                var bytes = Encoding.UTF8.GetBytes(csv);
                return File(bytes, "text/csv", fileDownloadName: string.Format(
                        "Locked Punches {0} thru {1}.csv",
                        commit.InAt.ToShortDateString(),
                        commit.OutAt.ToShortDateString()
                        ));
            }
            else if (InAt.HasValue && OutAt.HasValue)
            {
                var exportService = new ExportService(InAt.Value, OutAt.Value, currentUser.Id, _context);

                string csv = exportService.BuildCsv(Delimiter);
                var bytes = Encoding.UTF8.GetBytes(csv);
                return File(bytes, "text/csv", fileDownloadName: string.Format(
                        "All Punches {0} thru {1}.csv",
                        InAt.Value.ToShortDateString(),
                        OutAt.Value.ToShortDateString()
                        ));
            }
            else
            {
                return BadRequest("Must specify a date range or lock id to export punches.");
            }
        }

        private User CurrentUser()
        {
            var type = "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier";
            var sub = HttpContext.User.Claims.FirstOrDefault(c => c.Type == type).Value;
            var currentUserId = int.Parse(sub);
            return _context.Users
                .Where(u => u.Id == currentUserId)
                .FirstOrDefault();
        }
    }
}