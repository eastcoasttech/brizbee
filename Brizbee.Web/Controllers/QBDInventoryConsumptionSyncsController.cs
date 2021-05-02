using Brizbee.Common.Models;
using Dapper;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web;
using System.Web.Http;

namespace Brizbee.Web.Controllers
{
    public class QBDInventoryConsumptionSyncsController : ApiController
    {
        private SqlContext _context = new SqlContext();
        private JsonSerializerSettings settings = new JsonSerializerSettings()
        {
            NullValueHandling = NullValueHandling.Ignore,
            StringEscapeHandling = StringEscapeHandling.EscapeHtml,
            ReferenceLoopHandling = ReferenceLoopHandling.Ignore
        };

        // GET: api/QBDInventoryConsumptionSyncs
        [HttpGet]
        [Route("api/QBDInventoryConsumptionSyncs")]
        public HttpResponseMessage GetQBDInventoryConsumptions(
            [FromUri] int skip = 0, [FromUri] int pageSize = 1000,
            [FromUri] string orderBy = "QBDINVENTORYCONSUMPTIONSYNCS/CREATEDAT", [FromUri] string orderByDirection = "ASC")
        {
            if (pageSize > 1000) { Request.CreateResponse(HttpStatusCode.BadRequest); }

            var currentUser = CurrentUser();

            // Ensure Administrator.
            if (currentUser.Role != "Administrator")
                Request.CreateResponse(HttpStatusCode.BadRequest);

            var total = 0;
            List<QBDInventoryConsumptionSync> syncs = new List<QBDInventoryConsumptionSync>();
            using (var connection = new SqlConnection(ConfigurationManager.ConnectionStrings["SqlContext"].ToString()))
            {
                connection.Open();

                // Determine the order by columns.
                var orderByFormatted = "";
                switch (orderBy.ToUpperInvariant())
                {
                    case "QBDINVENTORYCONSUMPTIONSYNCS/CREATEDAT":
                        orderByFormatted = "S.[CreatedAt]";
                        break;
                    case "USERS/NAME":
                        orderByFormatted = "U.[Name]";
                        break;
                    default:
                        orderByFormatted = "S.[CreatedAt]";
                        break;
                }

                // Determine the order direction.
                var orderByDirectionFormatted = "";
                switch (orderByDirection.ToUpperInvariant())
                {
                    case "ASC":
                        orderByDirectionFormatted = "ASC";
                        break;
                    case "DESC":
                        orderByDirectionFormatted = "DESC";
                        break;
                    default:
                        orderByDirectionFormatted = "ASC";
                        break;
                }

                var parameters = new DynamicParameters();

                // Common clause.
                parameters.Add("@OrganizationId", currentUser.OrganizationId);

                // Get the count.
                var countSql = $@"
                    SELECT
                        COUNT(*)
                    FROM
                        [QBDInventoryConsumptionSyncs] AS S
                    WHERE
                        S.[OrganizationId] = @OrganizationId;";

                total = connection.QuerySingle<int>(countSql, parameters);

                // Paging parameters.
                parameters.Add("@Skip", skip);
                parameters.Add("@PageSize", pageSize);

                // Get the records.
                var recordsSql = $@"
                    SELECT
                        S.Id AS Sync_Id,
                        S.CreatedAt AS Sync_CreatedAt,
                        S.CreatedByUserId AS Sync_CreatedByUserId,
                        S.OrganizationId AS Sync_OrganizationId,
                        S.RecordingMethod AS Sync_RecordingMethod,
                        S.ValueMethod AS Sync_ValueMethod,
                        S.HostProductName AS Sync_HostProductName,
                        S.ConsumptionsCount AS Sync_ConsumptionsCount,

                        U.Id AS User_Id,
                        U.Name AS User_Name
                    FROM
                        [QBDInventoryConsumptionSyncs] AS S
                    INNER JOIN
                        [Users] AS U ON S.[CreatedByUserId] = U.[Id]
                    WHERE
                        S.[OrganizationId] = @OrganizationId
                    ORDER BY
                        {orderByFormatted} {orderByDirectionFormatted}
                    OFFSET @Skip ROWS
                    FETCH NEXT @PageSize ROWS ONLY;";

                var results = connection.Query<QBDInventoryConsumptionSyncExpanded>(recordsSql, parameters);

                foreach (var result in results)
                {
                    syncs.Add(new QBDInventoryConsumptionSync()
                    {
                        Id = result.Sync_Id,
                        CreatedAt = result.Sync_CreatedAt,
                        CreatedByUserId = result.Sync_CreatedByUserId,
                        OrganizationId = result.Sync_OrganizationId,
                        RecordingMethod = result.Sync_RecordingMethod,
                        ValueMethod = result.Sync_ValueMethod,
                        HostProductName = result.Sync_HostProductName,
                        ConsumptionsCount = result.Sync_ConsumptionsCount,
                        CreatedByUser = new User()
                        {
                            Id = result.User_Id,
                            Name = result.User_Name
                        }
                    });
                }

                connection.Close();
            }

            // Determine page count.
            int pageCount = total > 0
                ? (int)Math.Ceiling(total / (double)pageSize)
                : 0;

            // Create the response
            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(JsonConvert.SerializeObject(syncs, settings),
                    System.Text.Encoding.UTF8,
                    "application/json")
            };

            // Set headers for paging.
            response.Headers.Add("X-Paging-PageSize", pageSize.ToString(CultureInfo.InvariantCulture));
            response.Headers.Add("X-Paging-PageCount", pageCount.ToString(CultureInfo.InvariantCulture));
            response.Headers.Add("X-Paging-TotalRecordCount", total.ToString(CultureInfo.InvariantCulture));

            return response;
        }

        private User CurrentUser()
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

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _context.Dispose();
            }
            base.Dispose(disposing);
        }
    }

    public class QBDInventoryConsumptionSyncExpanded
    {
        // Sync Details

        public long Sync_Id { get; set; }

        public DateTime Sync_CreatedAt { get; set; }

        public int Sync_CreatedByUserId { get; set; }

        public int Sync_OrganizationId { get; set; }

        public string Sync_RecordingMethod { get; set; }

        public string Sync_ValueMethod { get; set; }

        public string Sync_HostProductName { get; set; }

        public int Sync_ConsumptionsCount { get; set; }


        // User Details

        public int User_Id { get; set; }

        public string User_Name { get; set; }
    }
}