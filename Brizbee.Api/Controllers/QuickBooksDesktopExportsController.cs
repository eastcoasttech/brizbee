//
//  QuickBooksDesktopExportsController.cs
//  BRIZBEE API
//
//  Copyright (C) 2019-2022 East Coast Technology Services, LLC
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

using Brizbee.Core.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData.Formatter;
using Microsoft.AspNetCore.OData.Query;
using Microsoft.AspNetCore.OData.Results;
using Microsoft.AspNetCore.OData.Routing.Controllers;
using Microsoft.EntityFrameworkCore;

namespace Brizbee.Api.Controllers
{
    public class QuickBooksDesktopExportsController : ODataController
    {
        private readonly SqlContext _context;

        public QuickBooksDesktopExportsController(SqlContext context)
        {
            _context = context;
        }

        // GET: odata/QuickBooksDesktopExports
        [HttpGet]
        [EnableQuery(PageSize = 20)]
        public IQueryable<QuickBooksDesktopExport> GetQuickBooksDesktopExports()
        {
            var currentUser = CurrentUser();

            return _context.QuickBooksDesktopExports!
                .Include(q => q.Commit)
                .Where(q => q.Commit!.OrganizationId == currentUser.OrganizationId);
        }

        // GET: odata/QuickBooksDesktopExports(5)
        [HttpGet]
        [EnableQuery]
        public SingleResult<QuickBooksDesktopExport> GetQuickBooksDesktopExport([FromODataUri] int key)
        {
            var currentUser = CurrentUser();

            var quickBooksDesktopExport = _context.QuickBooksDesktopExports!
                .Include(q => q.Commit)
                .Where(q => q.Commit!.OrganizationId == currentUser.OrganizationId)
                .Where(q => q.Id == key);

            return SingleResult.Create(quickBooksDesktopExport);
        }

        // POST: odata/QuickBooksDesktopExports
        [HttpPost]
        public IActionResult Post([FromBody] QuickBooksDesktopExport quickBooksDesktopExport)
        {
            var currentUser = CurrentUser();

            // Auto-generated.
            quickBooksDesktopExport.UserId = currentUser.Id;

            try
            {
                // Validate the model.
                ModelState.ClearValidationState(nameof(quickBooksDesktopExport));
                if (!TryValidateModel(quickBooksDesktopExport, nameof(quickBooksDesktopExport)))
                    return BadRequest();

                _context.QuickBooksDesktopExports!.Add(quickBooksDesktopExport);

                _context.SaveChanges();

                return Ok(quickBooksDesktopExport);
            }
            catch (DbUpdateException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        private User CurrentUser()
        {
            const string type = "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier";
            var claim = HttpContext.User.Claims.FirstOrDefault(c => c.Type == type)!.Value;
            var currentUserId = int.Parse(claim);
            return _context.Users!
                .FirstOrDefault(u => u.Id == currentUserId)!;
        }
    }
}
