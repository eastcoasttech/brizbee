using Brizbee.Common.Models;
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
        public HttpResponseMessage GetJobs([FromUri] int pageNumber = 1,
            [FromUri] string direction = "ASC", [FromUri] int pageSize = 1000, [FromUri] string orderBy = "JOBS/NAME",
            [FromUri] int[] jobIds = null, [FromUri] string[] jobNumbers = null, [FromUri] string[] jobNames = null,
            [FromUri] int[] customerIds = null, [FromUri] string[] customerNumbers = null, [FromUri] string[] customerNames = null)
        {
            // Determine the number of records to skip.
            int skip = (pageNumber - 1) * pageSize;

            var currentUser = CurrentUser();

            // Validate order.
            var allowed = new string[] { "JOBS/CREATEDAT", "JOBS/NAME", "JOBS/NUMBER" };
            if (!allowed.Contains(orderBy.ToUpperInvariant()))
            {
                //return BadRequest();
            }
            orderBy = "J.Name";

            // Validate direction.
            direction = direction.ToUpperInvariant();
            if (direction != "ASC" || direction != "DESC")
            {
                //return BadRequest();
            }

            // Build where clauses.
            var whereClauses = "";

            if (jobIds != null && jobIds.Length > 0)
            {
                whereClauses += $" AND J.Id IN ({string.Join(",", jobIds)})";
            }

            if (jobNumbers != null && jobNumbers.Length > 0)
            {
                whereClauses += $" AND J.Number IN ({string.Join(",", jobNumbers.Select(x => string.Format("'{0}'", x)))})";
            }

            if (jobNames != null && jobNames.Length > 0)
            {
                whereClauses += $" AND J.Name IN ({string.Join(",", jobNames.Select(x => string.Format("'{0}'", x)))})";
            }

            if (customerIds != null && customerIds.Length > 0)
            {
                whereClauses += $" AND C.Id IN ({string.Join(",", customerIds)})";
            }

            if (customerNumbers != null && customerNumbers.Length > 0)
            {
                whereClauses += $" AND C.Number IN ({string.Join(",", customerNumbers.Select(x => string.Format("'{0}'", x)))})";
            }

            if (customerNames != null && customerNames.Length > 0)
            {
                whereClauses += $" AND C.Name IN ({string.Join(",", customerNames.Select(x => string.Format("'{0}'", x)))})";
            }

            var records = new List<Job>();
            using (var connection = new SqlConnection(ConfigurationManager.ConnectionStrings["SqlContext"].ToString()))
            {
                connection.Open();

                var sql = $@"
                    SELECT
                        J.Id,
	                    J.CreatedAt,
                        J.Name,
                        J.Number,
                        J.Description,
                        J.QuickBooksCustomerJob,
                        J.QuoteNumber,
                        J.CustomerId,

                        C.Id,
	                    C.CreatedAt,
                        C.Name,
                        C.Number,
                        C.Description,
                        C.OrganizationId
                    FROM
	                    [Jobs] AS J
                    INNER JOIN
                        [Customers] AS C ON J.CustomerId = C.Id
                    WHERE
	                    C.[OrganizationId] = @OrganizationId
                        {whereClauses}
                    ORDER BY
	                    {orderBy} {direction}
                    OFFSET @Skip ROWS
                    FETCH NEXT @PageSize ROWS ONLY;";

                records = connection.Query<Job, Customer, Job>(sql, (j, c) =>
                {
                    j.Customer = c;
                    return j;
                },
                new { OrganizationId = currentUser.OrganizationId, Skip = skip, PageSize = pageSize }).ToList();

                connection.Close();
            }

            // Get total number of records
            var total = _context.Customers
                .Where(c => c.OrganizationId == currentUser.OrganizationId)
                .Count();

            // Determine page count
            int pageCount = total > 0
                ? (int)Math.Ceiling(total / (double)pageSize)
                : 0;

            // Create the response
            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(JsonConvert.SerializeObject(records, settings),
                    System.Text.Encoding.UTF8,
                    "application/json")
            };

            // Set headers for paging.
            //response.Headers.Add("X-Paging-PageNumber", pageNumber.ToString(CultureInfo.InvariantCulture));
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