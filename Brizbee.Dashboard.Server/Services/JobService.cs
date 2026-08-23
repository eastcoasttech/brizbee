using System.Net;
using System.Text;
using System.Text.Json;
using Brizbee.Core.Models;
using Brizbee.Core.Serialization.Statistics;
using Brizbee.Core.Transforms.Queries;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace Brizbee.Dashboard.Server.Services
{
    public class JobService(ApiService apiService,
        IDbContextFactory<PrimaryDbContext> dbContextFactory,
        SharedService sharedService)
    {
        private JsonSerializerOptions options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };

        public void ConfigureHeadersWithToken(string token)
        {
            // Clear old headers first
            ResetHeaders();

            apiService.GetHttpClient().DefaultRequestHeaders.Add("Authorization", $"Bearer {token}");
        }

        public void ResetHeaders()
        {
            apiService.GetHttpClient().DefaultRequestHeaders.Remove("Authorization");
        }

        public async Task<(List<Job>, long?)> GetExpandedJobsAsync(int pageSize = 100, int skip = 0, string sortBy = "Number", string sortDirection = "ASC")
        {
            var response = await apiService.GetHttpClient().GetAsync($"api/JobsExpanded?pageSize={pageSize}&skip={skip}&orderBy={sortBy}&orderByDirection={sortDirection}");

            if (!response.IsSuccessStatusCode)
                return (new List<Job>(0), 0);

            using var responseContent = await response.Content.ReadAsStreamAsync();
            var value = await JsonSerializer.DeserializeAsync<List<Job>>(responseContent, options);
            var total = long.Parse(response.Headers.GetValues("X-Paging-TotalRecordCount").FirstOrDefault());
            return (value, total);
        }

        public async Task<(List<Job>, long?)> GetJobsAsync(int customerId, int pageSize = 100, int skip = 0, string sortBy = "Number", string sortDirection = "ASC")
        {
            var response = await apiService.GetHttpClient().GetAsync($"odata/Jobs?$count=true&$top={pageSize}&$skip={skip}&$orderby={sortBy} {sortDirection}&$filter=CustomerId eq {customerId}");

            if (!response.IsSuccessStatusCode)
                return (new List<Job>(0), 0);

            using var responseContent = await response.Content.ReadAsStreamAsync();
            var odataResponse = await JsonSerializer.DeserializeAsync<ODataListResponse<Job>>(responseContent, options);
            return (odataResponse.Value.ToList(), odataResponse.Count);
        }

        public async Task<(List<Job>, long?)> GetFilteredJobsAsync(int pageSize = 100, int skip = 0, string sortBy = "JOBS/NUMBER", string sortDirection = "ASC", string filterStatus = "Open")
        {
            await using var context = await dbContextFactory.CreateDbContextAsync();

            // Ensure that user is authorized.
            //if (!currentUser.CanViewProjects)
            //    return Forbid();

            var total = 0;
            var jobs = new List<Job>();
            using (var connection = context.Database.GetDbConnection())
            {
                connection.Open();

                // Determine the order by columns.
                var orderByFormatted = "";
                switch (sortBy.ToUpperInvariant())
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
                    case "JOBS/STATUS":
                        orderByFormatted = "[J].[Status]";
                        break;
                    case "CUSTOMERS/NUMBER":
                        orderByFormatted = "[C].[Number]";
                        break;
                    case "CUSTOMERS/NAME":
                        orderByFormatted = "[C].[Name]";
                        break;
                    case "JOBS/CUSTOMER_WORK_ORDER":
                        orderByFormatted = "[J].[CustomerWorkOrder]";
                        break;
                    case "JOBS/CUSTOMER_PURCHASE_ORDER":
                        orderByFormatted = "[J].[CustomerPurchaseOrder]";
                        break;
                    case "JOBS/INVOICE_NUMBER":
                        orderByFormatted = "[J].[InvoiceNumber]";
                        break;
                    case "JOBS/QUOTE_NUMBER":
                        orderByFormatted = "[J].[QuoteNumber]";
                        break;
                    default:
                        orderByFormatted = "[J].[Name]";
                        break;
                }

                // Determine the order direction.
                var orderByDirectionFormatted = "";
                switch (sortDirection.ToUpperInvariant())
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
                parameters.Add("@OrganizationId", sharedService.CurrentUser?.OrganizationId);

                // Filter by status.
                whereClauses += $" AND [J].[Status] = '{filterStatus}'";

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
                        [J].[Status] AS [Job_Status],

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

                var results = connection.Query<JobWithCustomer>(recordsSql, parameters);

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
                        Status = result.Job_Status,

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

            return (jobs, total);
        }

        public async Task<(List<Job>, long?)> GetOpenJobsAsync(int pageSize = 100, int skip = 0, string sortBy = "Number", string sortDirection = "ASC")
        {
            var response = await apiService.GetHttpClient().GetAsync($"odata/Jobs/Open()?$expand=Customer&$count=true&$top={pageSize}&$skip={skip}&$orderby={sortBy} {sortDirection}");

            if (!response.IsSuccessStatusCode)
                return (new List<Job>(), 0);

            using var responseContent = await response.Content.ReadAsStreamAsync();
            var odataResponse = await JsonSerializer.DeserializeAsync<ODataListResponse<Job>>(responseContent, options);
            return (odataResponse.Value.ToList(), odataResponse.Count);
        }

        public async Task<(List<Job>, long?)> GetClosedJobsAsync(int pageSize = 100, int skip = 0, string sortBy = "Number", string sortDirection = "ASC")
        {
            var response = await apiService.GetHttpClient().GetAsync($"odata/Jobs/Closed()?$expand=Customer&$count=true&$top={pageSize}&$skip={skip}&$orderby={sortBy} {sortDirection}");

            if (!response.IsSuccessStatusCode)
                return (new List<Job>(), 0);

            using var responseContent = await response.Content.ReadAsStreamAsync();
            var odataResponse = await JsonSerializer.DeserializeAsync<ODataListResponse<Job>>(responseContent, options);
            return (odataResponse.Value.ToList(), odataResponse.Count);
        }

        public async Task<Job> GetJobByIdAsync(int id)
        {
            var response = await apiService.GetHttpClient().GetAsync($"odata/Jobs({id})?$expand=Customer");
            response.EnsureSuccessStatusCode();

            using var responseContent = await response.Content.ReadAsStreamAsync();
            return await JsonSerializer.DeserializeAsync<Job>(responseContent, options);
        }

        public async Task<ProjectStatistics> GetStatisticsAsync(int id)
        {
            var response = await apiService.GetHttpClient().GetAsync($"api/JobsExpanded/{id}/Statistics");
            response.EnsureSuccessStatusCode();

            using var responseContent = await response.Content.ReadAsStreamAsync();
            return await JsonSerializer.DeserializeAsync<ProjectStatistics>(responseContent, options);
        }

        public async Task<List<Job>> SearchJobsAsync(string query)
        {
            var response = await apiService.GetHttpClient().GetAsync($"odata/Jobs?$filter=contains(Name,'{query}')&$select=Name,Number,Id&$expand=Customer($select=Name,Number)");
            response.EnsureSuccessStatusCode();

            using var responseContent = await response.Content.ReadAsStreamAsync();
            var odataResponse = await JsonSerializer.DeserializeAsync<ODataListResponse<Job>>(responseContent, options);
            return odataResponse.Value.ToList();
        }

        public async Task<bool> DeleteJobAsync(int id)
        {
            var response = await apiService.GetHttpClient().DeleteAsync($"odata/Jobs({id})");
            if (response.IsSuccessStatusCode)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public async Task<Job> SaveJobAsync(Job job)
        {
            var url = job.Id != 0 ? $"odata/Jobs({job.Id})" : "odata/Jobs";
            var method = job.Id != 0 ? HttpMethod.Patch : HttpMethod.Post;

            using (var request = new HttpRequestMessage(method, url))
            {
                var payload = new Dictionary<string, object>() {
                    { "Name", job.Name },
                    { "Number", job.Number },
                    { "Description", job.Description },
                    { "Status", job.Status },
                    { "CustomerWorkOrder", job.CustomerWorkOrder },
                    { "CustomerPurchaseOrder", job.CustomerPurchaseOrder },
                    { "InvoiceNumber", job.InvoiceNumber },
                    { "QuoteNumber", job.QuoteNumber },
                    { "QuickBooksCustomerJob", job.QuickBooksCustomerJob },
                    { "QuickBooksClass", job.QuickBooksClass },
                    { "Taxability", job.Taxability }
                };

                // Can only be configured at creation.
                if (job.Id == 0)
                {
                    payload.Add("CustomerId", job.CustomerId);

                    // Optional, task template.
                    if (job.TaskTemplateId.HasValue)
                        payload.Add("TaskTemplateId", job.TaskTemplateId);
                }

                var json = JsonSerializer.Serialize(payload, options);

                using (var stringContent = new StringContent(json, Encoding.UTF8, "application/json"))
                {
                    request.Content = stringContent;

                    using (var response = await apiService
                        .GetHttpClient()
                        .SendAsync(request, HttpCompletionOption.ResponseHeadersRead)
                        .ConfigureAwait(false))
                    {
                        if (response.IsSuccessStatusCode)
                        {
                            using var responseContent = await response.Content.ReadAsStreamAsync();

                            if (response.StatusCode == HttpStatusCode.NoContent)
                            {
                                return null;
                            }
                            else
                            {
                                var deserialized = await JsonSerializer.DeserializeAsync<Job>(responseContent, options);
                                return deserialized;
                            }
                        }
                        else
                        {
                            return null;
                        }
                    }
                }
            }
        }

        public async Task<string> GetNextNumberAsync()
        {
            var response = await apiService.GetHttpClient().PostAsync("odata/Jobs/NextNumber", new StringContent(""));
            response.EnsureSuccessStatusCode();

            using var responseContent = await response.Content.ReadAsStreamAsync();
            var odataResponse = await JsonSerializer.DeserializeAsync<ODataSingleResponse<string>>(responseContent, options);
            return odataResponse.Value;
        }
    }
}
