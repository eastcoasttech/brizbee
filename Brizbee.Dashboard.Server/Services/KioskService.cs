using System.Text;
using Brizbee.Core.Models;
using Microsoft.EntityFrameworkCore;

namespace Brizbee.Dashboard.Server.Services
{
    public class KioskService(ApiService apiService, 
        IDbContextFactory<PrimaryDbContext> dbContextFactory,
        SharedService sharedService)
    {
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

        public async Task<bool> PunchInAsync(int taskId, string latitude, string longitude, string browserName, string browserVersion, string operationSystemName, string operationSystemVersion, string timeZone)
        {
            // Build the URL with query parameters.
            var url = new StringBuilder();
            url.Append("api/Kiosk/PunchIn?");
            url.Append($"taskId={taskId}&");
            url.Append($"timeZone={timeZone}&");
            url.Append($"latitude={latitude}&");
            url.Append($"longitude={longitude}&");
            url.Append("sourceHardware=Web&");
            url.Append($"sourceOperatingSystem={operationSystemName}&");
            url.Append($"sourceOperatingSystemVersion={operationSystemVersion}&");
            url.Append($"sourceBrowser={browserName}&");
            url.Append($"sourceBrowserVersion={browserVersion}");

            using (var request = new HttpRequestMessage(HttpMethod.Post, url.ToString()))
            {
                using (var response = await apiService
                    .GetHttpClient()
                    .SendAsync(request, HttpCompletionOption.ResponseHeadersRead)
                    .ConfigureAwait(false))
                {
                    if (response.IsSuccessStatusCode)
                        return true;
                    else
                        return false;
                }
            }
        }

        public async Task<bool> PunchOutAsync(string latitude, string longitude, string browserName, string browserVersion, string operationSystemName, string operationSystemVersion, string timeZone)
        {
            // Build the URL with query parameters.
            var url = new StringBuilder();
            url.Append("api/Kiosk/PunchOut?");
            url.Append($"timeZone={timeZone}&");
            url.Append($"latitude={latitude}&");
            url.Append($"longitude={longitude}&");
            url.Append("sourceHardware=Web&");
            url.Append($"sourceOperatingSystem={operationSystemName}&");
            url.Append($"sourceOperatingSystemVersion={operationSystemVersion}&");
            url.Append($"sourceBrowser={browserName}&");
            url.Append($"sourceBrowserVersion={browserVersion}");

            using (var request = new HttpRequestMessage(HttpMethod.Post, url.ToString()))
            {
                using (var response = await apiService
                        .GetHttpClient()
                        .SendAsync(request, HttpCompletionOption.ResponseHeadersRead)
                        .ConfigureAwait(false))
                {
                    if (response.IsSuccessStatusCode)
                        return true;
                    else
                        return false;
                }
            }
        }

        public async Task<Punch?> GetCurrentPunchAsync()
        {
            await using var context = await dbContextFactory.CreateDbContextAsync();
            
            var currentUser = sharedService.CurrentUser;

            if (currentUser == null)
                return null;
            
            var punch = await context.Punches!
                .Include(p => p.Task)
                .Include(p => p.Task!.Job)
                .Include(p => p.Task!.Job!.Customer)
                .Where(p => p.UserId == currentUser.Id)
                .Where(p => !p.OutAt.HasValue)
                .OrderByDescending(p => p.InAt)
                .FirstOrDefaultAsync();

            return punch;
        }

        public async Task<bool> AddTimeCardAsync(DateTime enteredAt, int minutes, string notes, int taskId)
        {
            await using var context = await dbContextFactory.CreateDbContextAsync();

            var currentUser = sharedService.CurrentUser;

            if (currentUser == null)
                return false;
            
            var task = await context.Tasks!
                .Include(t => t.Job!.Customer)
                .Where(t => t.Job!.Customer!.OrganizationId == currentUser.OrganizationId)
                .Where(t => t.Id == taskId)
                .FirstOrDefaultAsync();

            if (task == null)
                return false;
            
            // Ensure job is open.
            if (task.Job!.Status != "Open")
                return false;
            
            try
            {
                var timesheetEntry = new TimesheetEntry
                {
                    CreatedAt = DateTime.UtcNow,
                    EnteredAt = enteredAt,
                    Minutes = minutes,
                    Notes = notes,
                    TaskId = taskId,
                    UserId = currentUser.Id
                };
                context.TimesheetEntries!.Add(timesheetEntry);
                await context.SaveChangesAsync();

                return true;
            }
            catch (DbUpdateException)
            {
                return false;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public async Task<Core.Models.Task?> SearchTasksAsync(string taskNumber)
        {
            await using var context = await dbContextFactory.CreateDbContextAsync();

            var currentUser = sharedService.CurrentUser;

            if (currentUser == null)
                return null;
            
            var task = await context.Tasks!
                .Include(t => t.Job)
                .Include(t => t.Job!.Customer)
                .Where(t => t.Job!.Customer!.OrganizationId == currentUser.OrganizationId)
                .Where(t => t.Number == taskNumber)
                .FirstOrDefaultAsync();

            return task;
        }

        public async Task<List<Customer>?> GetCustomersAsync()
        {
            await using var context = await dbContextFactory.CreateDbContextAsync();

            var currentUser = sharedService.CurrentUser;

            if (currentUser == null)
                return null;
            
            var customers = await context.Customers!
                .Where(c => c.OrganizationId == currentUser.OrganizationId)
                .OrderBy(c => c.Number)
                .ToListAsync();

            return customers;
        }

        public async Task<List<Job>?> GetProjectsAsync(int customerId)
        {
            await using var context = await dbContextFactory.CreateDbContextAsync();

            var currentUser = sharedService.CurrentUser;

            if (currentUser == null)
                return null;

            var projects = await context.Jobs!
                .Include(j => j.Customer)
                .Where(j => j.Customer!.OrganizationId == currentUser.OrganizationId)
                .Where(j => j.CustomerId == customerId)
                .OrderBy(j => j.Number)
                .ToListAsync();

            return projects;
        }

        public async Task<List<Core.Models.Task>?> GetTasksAsync(int projectId)
        {
            await using var context = await dbContextFactory.CreateDbContextAsync();

            var currentUser = sharedService.CurrentUser;

            if (currentUser == null)
                return null;

            var tasks = await context.Tasks!
                .Include(t => t.Job!.Customer)
                .Where(t => t.Job!.Customer!.OrganizationId == currentUser.OrganizationId)
                .Where(t => t.JobId == projectId)
                .OrderBy(t => t.Number)
                .ToListAsync();
            
            return tasks;
        }
    }
}
