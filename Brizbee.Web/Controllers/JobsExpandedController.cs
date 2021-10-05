using Brizbee.Common.Models;
using Brizbee.Web.Serialization.Expanded;
using CsvHelper;
using CsvHelper.Configuration;
using Dapper;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Web.Http;
using static Brizbee.Web.Controllers.ReportsController;

namespace Brizbee.Web.Controllers
{
    public class JobsExpandedController : ApiController
    {
        private SqlContext _context = new SqlContext();
        private JsonSerializerSettings settings = new JsonSerializerSettings()
        {
            NullValueHandling = NullValueHandling.Ignore,
            StringEscapeHandling = StringEscapeHandling.EscapeHtml,
            ReferenceLoopHandling = ReferenceLoopHandling.Ignore
        };

        // GET: api/JobsExpanded
        [HttpGet]
        [Route("api/JobsExpanded")]
        public HttpResponseMessage GetJobs(
            [FromUri] int skip = 0, [FromUri] int pageSize = 1000,
            [FromUri] string orderBy = "JOBS/NAME", [FromUri] string orderByDirection = "ASC",
            [FromUri] int[] jobIds = null, [FromUri] string[] jobNumbers = null, [FromUri] string[] jobNames = null,
            [FromUri] int[] customerIds = null, [FromUri] string[] customerNumbers = null, [FromUri] string[] customerNames = null)
        {
            if (pageSize > 1000) { Request.CreateResponse(HttpStatusCode.BadRequest); }

            var currentUser = CurrentUser();

            // Ensure that user is authorized.
            if (!currentUser.CanViewProjects)
                Request.CreateResponse(HttpStatusCode.Forbidden);

            var total = 0;
            var jobs = new List<Job>();
            using (var connection = new SqlConnection(ConfigurationManager.ConnectionStrings["SqlContext"].ToString()))
            {
                connection.Open();

                // Determine the order by columns.
                var orderByFormatted = "";
                switch (orderBy.ToUpperInvariant())
                {
                    case "JOBS/CREATEDAT":
                        orderByFormatted = "[J].[CreatedAt]";
                        break;
                    case "JOBS/NUMBER":
                        orderByFormatted = "[J].[Number]";
                        break;
                    case "JOBS/NAME":
                        orderByFormatted = "[J].[Name]";
                        break;
                    case "CUSTOMERS/NUMBER":
                        orderByFormatted = "[C].[Number]";
                        break;
                    case "CUSTOMERS/NAME":
                        orderByFormatted = "[C].[Name]";
                        break;
                    default:
                        orderByFormatted = "[J].[Name]";
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

                var whereClauses = "";
                var parameters = new DynamicParameters();

                // Common clause.
                parameters.Add("@OrganizationId", currentUser.OrganizationId);

                // Clause for job ids.
                if (jobIds != null && jobIds.Length > 0)
                {
                    whereClauses += $" AND [J].[Id] IN ({string.Join(",", jobIds)})";
                }

                // Clause for job numbers.
                if (jobNumbers != null && jobNumbers.Length > 0)
                {
                    whereClauses += $" AND [J].[Number] IN ({string.Join(",", jobNumbers.Select(x => string.Format("'{0}'", x)))})";
                }

                // Clause for job names.
                if (jobNames != null && jobNames.Length > 0)
                {
                    whereClauses += $" AND [J].[Name] IN ({string.Join(",", jobNames.Select(x => string.Format("'{0}'", x)))})";
                }

                // Clause for customer ids.
                if (customerIds != null && customerIds.Length > 0)
                {
                    whereClauses += $" AND [C].[Id] IN ({string.Join(",", customerIds)})";
                }

                // Clause for customer numbers.
                if (customerNumbers != null && customerNumbers.Length > 0)
                {
                    whereClauses += $" AND [C].[Number] IN ({string.Join(",", customerNumbers.Select(x => string.Format("'{0}'", x)))})";
                }

                // Clause for customer names.
                if (customerNames != null && customerNames.Length > 0)
                {
                    whereClauses += $" AND [C].[Name] IN ({string.Join(",", customerNames.Select(x => string.Format("'{0}'", x)))})";
                }

                // Get the count.
                var countSql = $@"
                    SELECT
                        COUNT(*)
                    FROM
                        [Jobs] AS [J]
                    INNER JOIN
                        [Customers] AS [C] ON [J].[CustomerId] = [C].[Id]
                    WHERE
                        [C].[OrganizationId] = @OrganizationId {whereClauses};";

                total = connection.QuerySingle<int>(countSql, parameters);

                // Paging parameters.
                parameters.Add("@Skip", skip);
                parameters.Add("@PageSize", pageSize);

                // Get the records.
                var recordsSql = $@"
                    SELECT
                        [J].[Id] AS [Job_Id],
	                    [J].[CreatedAt] AS [Job_CreatedAt],
                        [J].[Name] AS [Job_Name],
                        [J].[Number] AS [Job_Number],
                        [J].[Description] AS [Job_Description],
                        [J].[QuickBooksCustomerJob] AS [Job_QuickBooksCustomerJob],
                        [J].[QuoteNumber] AS [Job_QuoteNumber],
                        [J].[CustomerId] AS [Job_CustomerId],

                        [C].[Id] AS [Customer_Id],
	                    [C].[CreatedAt] AS [Customer_CreatedAt],
                        [C].[Name] AS [Customer_Name],
                        [C].[Number] AS [Customer_Number],
                        [C].[Description] AS [Customer_Description],
                        [C].[OrganizationId] AS [Customer_OrganizationId]
                    FROM
	                    [Jobs] AS [J]
                    INNER JOIN
                        [Customers] AS [C] ON [J].[CustomerId] = [C].[Id]
                    WHERE
	                    [C].[OrganizationId] = @OrganizationId {whereClauses}
                    ORDER BY
                        {orderByFormatted} {orderByDirectionFormatted}
                    OFFSET @Skip ROWS
                    FETCH NEXT @PageSize ROWS ONLY;";

                var results = connection.Query<JobExpanded>(recordsSql, parameters);

                foreach (var result in results)
                {
                    jobs.Add(new Job()
                    {
                        Id = result.Job_Id,
                        CreatedAt = result.Job_CreatedAt,
                        Name = result.Job_Name,
                        Number = result.Job_Number,
                        Description = result.Job_Description,
                        QuickBooksCustomerJob = result.Job_QuickBooksCustomerJob,
                        QuoteNumber = result.Job_QuoteNumber,
                        CustomerId = result.Job_CustomerId,

                        Customer = new Customer()
                        {
                            Id = result.Customer_Id,
                            CreatedAt = result.Customer_CreatedAt,
                            Name = result.Customer_Name,
                            Number = result.Customer_Number,
                            Description = result.Customer_Description,
                            OrganizationId = result.Customer_OrganizationId
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
                Content = new StringContent(JsonConvert.SerializeObject(jobs, settings),
                    Encoding.UTF8,
                    "application/json")
            };

            // Set headers for paging.
            response.Headers.Add("X-Paging-PageSize", pageSize.ToString(CultureInfo.InvariantCulture));
            response.Headers.Add("X-Paging-PageCount", pageCount.ToString(CultureInfo.InvariantCulture));
            response.Headers.Add("X-Paging-TotalRecordCount", total.ToString(CultureInfo.InvariantCulture));

            return response;
        }

        // GET: api/JobsExpanded/Export
        [HttpGet]
        [Route("api/JobsExpanded/Export")]
        public IHttpActionResult Export()
        {
            var currentUser = CurrentUser();

            var jobs = _context.Jobs
                .Include("Customer")
                .Where(j => j.Customer.OrganizationId == currentUser.OrganizationId)
                .Where(j => j.Status != "Closed")
                .Where(j => j.Status != "Merged")
                .Select(j => new
                {
                    CustomerNumber = j.Customer.Number,
                    CustomerName = j.Customer.Name,
                    ProjectNumber = j.Number,
                    ProjectName = j.Name,
                    j.Status,
                    j.QuoteNumber,
                    j.CustomerWorkOrder,
                    j.CustomerPurchaseOrder,
                    j.InvoiceNumber
                })
                .ToList();

            var configuration = new CsvConfiguration(CultureInfo.CurrentCulture)
            {
                Delimiter = ","
            };
            using (var writer = new StringWriter())
            using (var csv = new CsvWriter(writer, configuration))
            {
                csv.WriteRecords(jobs);

                var bytes = Encoding.UTF8.GetBytes(writer.ToString());
                return new FileActionResult(bytes, "text/csv",
                    "Open Projects.csv",
                    Request);
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