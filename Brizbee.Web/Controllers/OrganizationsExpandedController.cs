//
//  OrganizationsExpandedController.cs
//  BRIZBEE API
//
//  Copyright (C) 2020 East Coast Technology Services, LLC
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
using Brizbee.Common.Models;
using Brizbee.Common.Serialization.Alerts;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.Description;

namespace Brizbee.Web.Controllers
{
    public class OrganizationsExpandedController : ApiController
    {
        private SqlContext _context = new SqlContext();
        private JsonSerializerSettings settings = new JsonSerializerSettings()
        {
            NullValueHandling = NullValueHandling.Ignore,
            StringEscapeHandling = StringEscapeHandling.EscapeHtml,
            ReferenceLoopHandling = ReferenceLoopHandling.Ignore
        };

        // GET: api/OrganizationsExpanded/5/Alerts
        [HttpGet]
        [Route("api/OrganizationsExpanded/{id}/Alerts")]
        [ResponseType(typeof(List<Alert>))]
        public async Task<IHttpActionResult> GetAlerts(int id)
        {
            var currentUser = CurrentUser();

            var organization = _context.Organizations.Find(id);

            // Ensure that object was found.
            if (organization == null) return NotFound();

            // Ensure that user is authorized.
            if (currentUser.Role != "Administrator" ||
                currentUser.OrganizationId != id)
                return BadRequest();

            try
            {
                // Download and deserialize the json.
                var azureConnectionString = ConfigurationManager.AppSettings["AlertsAzureStorageConnectionString"].ToString();
                BlobServiceClient blobServiceClient = new BlobServiceClient(azureConnectionString);
                BlobContainerClient containerClient = blobServiceClient.GetBlobContainerClient("alerts");
                BlobClient blobClient = containerClient.GetBlobClient($"{organization.Id}.json");

                List<Alert> result;

                using (var stream = await blobClient.OpenReadAsync())
                using (var sr = new StreamReader(stream))
                using (var jr = new JsonTextReader(sr))
                {
                    result = JsonSerializer.CreateDefault().Deserialize<List<Alert>>(jr);
                }

                return Ok(result);
            }
            catch (Exception ex)
            {
                Trace.TraceError(ex.ToString());
                return BadRequest(ex.Message);
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _context.Dispose();
            }
            base.Dispose(disposing);
        }

        public User CurrentUser()
        {
            if (ActionContext.RequestContext.Principal.Identity.Name.Length > 0)
            {
                var currentUserId = int.Parse(ActionContext.RequestContext.Principal.Identity.Name);
                return _context.Users
                    .Where(u => u.Id == currentUserId)
                    .FirstOrDefault();
            }
            else
            {
                return null;
            }
        }
    }
}
