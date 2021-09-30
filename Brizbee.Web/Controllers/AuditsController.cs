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
using System.Web.Http;

namespace Brizbee.Web.Controllers
{
    public class AuditsController : ApiController
    {
        private SqlContext _context = new SqlContext();
        private JsonSerializerSettings settings = new JsonSerializerSettings()
        {
            NullValueHandling = NullValueHandling.Ignore,
            StringEscapeHandling = StringEscapeHandling.EscapeHtml,
            ReferenceLoopHandling = ReferenceLoopHandling.Ignore
        };

        // GET: api/Audits
        [HttpGet]
        [Route("api/Audits")]
        public HttpResponseMessage GetAudits([FromUri] int skip = 0, [FromUri] int pageSize = 1000,
            [FromUri] string orderBy = "AUDITS/NUMBER", [FromUri] string orderByDirection = "ASC",
            [FromUri] int[] userIds = null)
        {
            var currentUser = CurrentUser();

            //// Ensure that user is authorized.
            //if (!currentUser.CanViewAudits)
            //    Request.CreateResponse(HttpStatusCode.BadRequest);

            var total = 0;
            List<Task> tasks = new List<Task>(0);
            using (var connection = new SqlConnection(ConfigurationManager.ConnectionStrings["SqlContext"].ToString()))
            {
                connection.Open();

                // Determine the order by columns.
                var orderByFormatted = "";
                switch (orderBy.ToUpperInvariant())
                {
                    case "JOBS/NUMBER":
                        orderByFormatted = "[J].[Number]";
                        break;
                    case "JOBS/NAME":
                        orderByFormatted = "[J].[Name]";
                        break;
                    case "TASKS/NUMBER":
                        orderByFormatted = "[T].[Number]";
                        break;
                    case "TASKS/NAME":
                        orderByFormatted = "[T].[Name]";
                        break;
                    default:
                        orderByFormatted = "[T].[NUMBER]";
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

                // Get the count.
                var countSql = $@"
                    SELECT
                        COUNT(DISTINCT([P].[TaskId]))
                    FROM
                        [Punches] AS [P]
                    INNER JOIN
                        [Users] AS [U] ON [P].[UserId] = [U].[Id]
                    WHERE
	                    [U].[OrganizationId] = @OrganizationId AND
	                    CAST([P].[InAt] AS DATE) BETWEEN @Min AND @Max;";

                total = connection.QueryFirst<int>(countSql, new
                {
                    OrganizationId = currentUser.OrganizationId
                });

                // Get the records.
                var recordsSql = $@"
                    SELECT
                        [T].[Id] AS Task_Id,
                        [T].[CreatedAt] AS Task_CreatedAt,
                        [T].[JobId] AS Task_JobId,
                        [T].[Name] AS Task_Name,
                        [T].[Number] AS Task_Number,
                        [T].[QuickBooksPayrollItem] AS Task_QuickBooksPayrollItem,
                        [T].[QuickBooksServiceItem] AS Task_QuickBooksServiceItem,
                        [T].[BaseServiceRateId] AS Task_BaseServiceRateId,
                        [T].[BasePayrollRateId] AS Task_BasePayrollRateId,

                        [J].[Id] AS Job_Id,
                        [J].[CreatedAt] AS Job_CreatedAt,
                        [J].[CustomerId] AS Job_CustomerId,
                        [J].[Description] AS Job_Description,
                        [J].[Name] AS Job_Name,
                        [J].[Number] AS Job_Number,
                        [J].[QuickBooksCustomerJob] AS Job_QuickBooksCustomerJob,

                        [C].[Id] AS Customer_Id,
                        [C].[CreatedAt] AS Customer_CreatedAt,
                        [C].[Description] AS Customer_Description,
                        [C].[Name] AS Customer_Name,
                        [C].[Number] AS Customer_Number,
                        [C].[OrganizationId] AS Customer_OrganizationId,

                        [PR].[Id] AS BasePayrollRate_Id,
                        [PR].[CreatedAt] AS BasePayrollRate_CreatedAt,
                        [PR].[IsDeleted] AS BasePayrollRate_IsDeleted,
                        [PR].[Name] AS BasePayrollRate_Name,
                        [PR].[OrganizationId] AS BasePayrollRate_OrganizationId,
                        [PR].[ParentRateId] AS BasePayrollRate_ParentRateId,
                        [PR].[QBDPayrollItem] AS BasePayrollRate_QBDPayrollItem,
                        [PR].[QBOPayrollItem] AS BasePayrollRate_QBOPayrollItem,
                        [PR].[Type] AS BasePayrollRate_Type,

                        [SR].[Id] AS BaseServiceRate_Id,
                        [SR].[CreatedAt] AS BaseServiceRate_CreatedAt,
                        [SR].[IsDeleted] AS BaseServiceRate_IsDeleted,
                        [SR].[Name] AS BaseServiceRate_Name,
                        [SR].[OrganizationId] AS BaseServiceRate_OrganizationId,
                        [SR].[ParentRateId] AS BaseServiceRate_ParentRateId,
                        [SR].[QBDServiceItem] AS BaseServiceRate_QBDServiceItem,
                        [SR].[QBOServiceItem] AS BaseServiceRate_QBOServiceItem,
                        [SR].[Type] AS BaseServiceRate_Type
                    FROM
                        [Tasks] AS [T]
                    INNER JOIN
                        [Jobs] AS [J] ON [J].[Id] = [T].[JobId]
                    INNER JOIN
                        [Customers] AS [C] ON [C].[Id] = [J].[CustomerId]
                    LEFT OUTER JOIN
                        [Rates] AS [PR] ON [T].[BasePayrollRateId] = [PR].[Id]
                    LEFT OUTER JOIN
                        [Rates] AS [SR] ON [T].[BaseServiceRateId] = [SR].[Id]
                    WHERE
                        [T].[Id] IN
                        (
                            SELECT
	                            [T].[Id]
                            FROM
	                            [Punches] AS [P]
                            INNER JOIN
	                            [Tasks] AS [T] ON [T].[Id] = [P].[TaskId]
                            INNER JOIN
	                            [Users] AS [U] ON [U].[Id] = [P].[UserId]
                            WHERE
	                            [U].[OrganizationId] = @OrganizationId AND
	                            CAST([P].[InAt] AS DATE) BETWEEN @Min AND @Max
                            GROUP BY
	                            [T].[Id]
                        )
                    ORDER BY
                        {orderByFormatted} {orderByDirectionFormatted};";

                var results = connection.Query<TaskExpanded>(recordsSql, new
                {
                    OrganizationId = currentUser.OrganizationId,
                    Min = min,
                    Max = max
                });

                foreach (var result in results)
                {
                    tasks.Add(new Task()
                    {
                        Id = result.Task_Id,
                        CreatedAt = result.Task_CreatedAt,
                        JobId = result.Task_JobId,
                        Name = result.Task_Name,
                        Number = result.Task_Number,
                        QuickBooksPayrollItem = result.Task_QuickBooksPayrollItem,
                        QuickBooksServiceItem = result.Task_QuickBooksServiceItem,
                        BaseServiceRateId = result.Task_BaseServiceRateId,
                        BasePayrollRateId = result.Task_BasePayrollRateId,
                        Job = new Job()
                        {
                            Id = result.Job_Id,
                            CreatedAt = result.Job_CreatedAt,
                            CustomerId = result.Job_CustomerId,
                            Description = result.Job_Description,
                            Name = result.Job_Name,
                            Number = result.Job_Number,
                            QuickBooksCustomerJob = result.Job_QuickBooksCustomerJob,
                            Customer = new Customer()
                            {
                                Id = result.Customer_Id,
                                CreatedAt = result.Customer_CreatedAt,
                                Description = result.Customer_Description,
                                Name = result.Customer_Name,
                                Number = result.Customer_Number,
                                OrganizationId = result.Customer_OrganizationId
                            }
                        },
                        BasePayrollRate = new Rate()
                        {
                            Id = result.BasePayrollRate_Id.GetValueOrDefault(),
                            IsDeleted = result.BasePayrollRate_IsDeleted.GetValueOrDefault(),
                            CreatedAt = result.BasePayrollRate_CreatedAt.GetValueOrDefault(),
                            Name = result.BasePayrollRate_Name,
                            OrganizationId = result.BasePayrollRate_OrganizationId.GetValueOrDefault(),
                            ParentRateId = result.BasePayrollRate_ParentRateId,
                            QBDPayrollItem = result.BasePayrollRate_QBDPayrollItem,
                            QBOPayrollItem = result.BasePayrollRate_QBOPayrollItem,
                            Type = result.BasePayrollRate_Type
                        },
                        BaseServiceRate = new Rate()
                        {
                            Id = result.BaseServiceRate_Id.GetValueOrDefault(),
                            IsDeleted = result.BaseServiceRate_IsDeleted.GetValueOrDefault(),
                            CreatedAt = result.BaseServiceRate_CreatedAt.GetValueOrDefault(),
                            Name = result.BaseServiceRate_Name,
                            OrganizationId = result.BaseServiceRate_OrganizationId.GetValueOrDefault(),
                            ParentRateId = result.BaseServiceRate_ParentRateId,
                            QBDServiceItem = result.BaseServiceRate_QBDServiceItem,
                            QBOServiceItem = result.BaseServiceRate_QBOServiceItem,
                            Type = result.BaseServiceRate_Type
                        },
                    });
                }

                connection.Close();
            }

            // Create the response
            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(JsonConvert.SerializeObject(tasks, settings),
                    System.Text.Encoding.UTF8,
                    "application/json")
            };

            // Set headers.
            response.Headers.Add("X-Paging-TotalRecordCount", total.ToString(CultureInfo.InvariantCulture));

            return response;
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
