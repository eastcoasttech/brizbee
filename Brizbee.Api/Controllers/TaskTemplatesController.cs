//
//  TaskTemplatesController.cs
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

using Brizbee.Core.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData.Formatter;
using Microsoft.AspNetCore.OData.Query;
using Microsoft.AspNetCore.OData.Results;
using Microsoft.AspNetCore.OData.Routing.Controllers;

namespace Brizbee.Api.Controllers
{
    public class TaskTemplatesController : ODataController
    {
        private readonly IConfiguration _configuration;
        private readonly SqlContext _context;

        public TaskTemplatesController(IConfiguration configuration, SqlContext context)
        {
            _configuration = configuration;
            _context = context;
        }

        // GET: odata/TaskTemplates
        [EnableQuery(PageSize = 20, MaxExpansionDepth = 1)]
        public IQueryable<TaskTemplate> GetTaskTemplates()
        {
            var currentUser = CurrentUser();

            // Ensure that user is authorized.
            if (!currentUser.CanCreateProjects)
                return new List<TaskTemplate>().AsQueryable();

            return _context.TaskTemplates
                .Where(t => t.OrganizationId == currentUser.OrganizationId);
        }

        // GET: odata/TaskTemplates(5)
        [EnableQuery]
        public SingleResult<TaskTemplate> GetTaskTemplate([FromODataUri] int key)
        {
            var currentUser = CurrentUser();

            // Ensure that user is authorized.
            if (!currentUser.CanCreateProjects)
                return SingleResult.Create(new List<TaskTemplate>().AsQueryable());

            return SingleResult.Create(_context.TaskTemplates
                .Where(t => t.OrganizationId == currentUser.OrganizationId)
                .Where(t => t.Id == key));
        }

        // POST: odata/TaskTemplates
        public IActionResult Post([FromBody] TaskTemplate taskTemplate)
        {
            var currentUser = CurrentUser();

            // Ensure that user is authorized.
            if (!currentUser.CanCreateProjects)
                return BadRequest();

            // Auto-generated.
            taskTemplate.CreatedAt = DateTime.UtcNow;
            taskTemplate.OrganizationId = currentUser.OrganizationId;

            // Validate the model.
            ModelState.ClearValidationState(nameof(taskTemplate));
            if (!TryValidateModel(taskTemplate, nameof(taskTemplate)))
                return BadRequest();

            _context.TaskTemplates.Add(taskTemplate);

            _context.SaveChanges();

            return Ok(taskTemplate);
        }

        // DELETE: odata/TaskTemplates(5)
        public IActionResult Delete([FromODataUri] int key)
        {
            var currentUser = CurrentUser();

            var taskTemplate = _context.TaskTemplates.Find(key);

            // Ensure that object was found.
            if (taskTemplate == null) return NotFound();

            // Ensure that user is authorized.
            if (!currentUser.CanCreateProjects ||
                taskTemplate.OrganizationId != currentUser.OrganizationId)
                return BadRequest();

            // Delete the object itself.
            _context.TaskTemplates.Remove(taskTemplate);

            _context.SaveChanges();

            return NoContent();
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
