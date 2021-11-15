//
//  OrganizationsExpandedController.cs
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

using Azure.Storage.Blobs;
using Brizbee.Api;
using Brizbee.Core.Models;
using Brizbee.Core.Serialization.Alerts;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using System.Text.Json;

namespace Brizbee.Api.Controllers
{
    public class OrganizationsExpandedController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        private readonly SqlContext _context;

        public OrganizationsExpandedController(IConfiguration configuration, SqlContext context)
        {
            _configuration = configuration;
            _context = context;
        }

        // GET: api/OrganizationsExpanded/5/Alerts
        [HttpGet]
        [Route("api/OrganizationsExpanded/{id}/Alerts")]
        [ProducesResponseType(typeof(List<Alert>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetAlerts(int id)
        {
            var currentUser = CurrentUser();

            var organization = _context.Organizations.Find(id);

            // Ensure that object was found.
            if (organization == null) return NotFound();

            // Ensure that user is authorized.
            if (!currentUser.CanViewPunches ||
                currentUser.OrganizationId != id)
                return BadRequest();

            try
            {
                // Download and deserialize the json.
                var azureConnectionString = _configuration["AlertsAzureStorageConnectionString"];
                BlobServiceClient blobServiceClient = new BlobServiceClient(azureConnectionString);
                BlobContainerClient containerClient = blobServiceClient.GetBlobContainerClient("alerts");
                BlobClient blobClient = containerClient.GetBlobClient($"{organization.Id}.json");

                List<Alert> result;

                using (var stream = await blobClient.OpenReadAsync())
                {
                    result = JsonSerializer.Deserialize<List<Alert>>(stream);
                }

                return Ok(result);
            }
            catch (Exception ex)
            {
                Trace.TraceError(ex.ToString());
                return BadRequest(ex.Message);
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
