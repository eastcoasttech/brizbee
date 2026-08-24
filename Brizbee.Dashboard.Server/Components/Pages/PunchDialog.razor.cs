using System.Globalization;
using System.Text.Json;
using Brizbee.Core.Models;
using Brizbee.Dashboard.Server.Serialization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using NodaTime;
using NodaTime.TimeZones;
using Radzen;

namespace Brizbee.Dashboard.Server.Components.Pages
{
    public partial class PunchDialog : ComponentBase
    {
        [Parameter] public int? Id { get; set; }

        private bool working = false;
        private bool loading = true;
        private bool loadingUsers = true;
        private bool loadingCustomers = true;
        private bool loadingJobs = true;
        private bool loadingTasks = true;
        private bool loadingAudits = true;
        private List<Country> countries = new List<Country>();
        private List<IanaTimeZone> zones = new List<IanaTimeZone>();
        private List<IanaTimeZone> inAtZones = new List<IanaTimeZone>();
        private List<IanaTimeZone> outAtZones = new List<IanaTimeZone>();
        private Punch punch = new Punch();
        private int selectedUserId;
        private int selectedCustomerId;
        private int selectedJobId;
        private int selectedTaskId;
        private string inAtHour = "9";
        private string inAtMinute = "00";
        private string inAtMeridian = "AM";
        private string selectedInAtCountryCode = "";
        private string selectedInAtTimeZone = "";
        private string outAtHour = "5";
        private string outAtMinute = "00";
        private string outAtMeridian = "PM";
        private string selectedOutAtCountryCode = "";
        private string selectedOutAtTimeZone = "";
        private bool hasPunchOut = false;
        private List<Customer> customers = new List<Customer>();
        private List<Brizbee.Core.Models.Task> tasks = new List<Brizbee.Core.Models.Task>();
        private List<Job> jobs = new List<Job>();
        private List<User> users = new List<User>();
        private List<Audit> audits = new List<Audit>();
        private string selectedTab = "DETAILS";
        private GoogleMapPosition center = null;
        private Audit selectedAudit = null;
        private Punch selectedBefore = null;
        private Punch selectedAfter = null;

        protected override async System.Threading.Tasks.Task OnInitializedAsync()
        {
            // --------------------------------------------------------------------
            // Build list of time zones.
            // --------------------------------------------------------------------

            var now = SystemClock.Instance.GetCurrentInstant();
            var tzdb = DateTimeZoneProviders.Tzdb;
            var countryCode = "";

            var list =
                from location in TzdbDateTimeZoneSource.Default.ZoneLocations
                where string.IsNullOrEmpty(countryCode) ||
                      location.CountryCode.Equals(countryCode,
                        StringComparison.OrdinalIgnoreCase)
                let zoneId = location.ZoneId
                let tz = tzdb[zoneId]
                let offset = tz.GetZoneInterval(now).StandardOffset
                orderby offset, zoneId
                select new
                {
                    Id = zoneId,
                    CountryCode = location.CountryCode
                };

            foreach (var z in list)
            {
                zones.Add(new IanaTimeZone() { Id = z.Id, CountryCode = z.CountryCode });
            }


            // --------------------------------------------------------------------
            // Build list of countries.
            // --------------------------------------------------------------------

            CultureInfo[] cultures = CultureInfo.GetCultures(CultureTypes.SpecificCultures);

            foreach (var culture in cultures)
            {
                try
                {
                    var region = new RegionInfo(culture.Name);
                    if (!countries.Where(c => c.Name == region.EnglishName).Any())
                    {
                        countries.Add(new Country() { CountryCode = region.TwoLetterISORegionName, Name = region.EnglishName });
                    }
                }
                catch (CultureNotFoundException ex)
                {
                    Console.WriteLine(ex.Message);
                }
            }


            // --------------------------------------------------------------------
            // Set defaults.
            // --------------------------------------------------------------------

            punch = new Punch()
            {
                InAt = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, 9, 0, 0),
                OutAt = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, 17, 0, 0)
            };

            selectedInAtCountryCode = "US";
            selectedInAtCountryCodeValueChangeHandler(selectedInAtCountryCode);
            selectedOutAtCountryCode = "US";
            selectedOutAtCountryCodeValueChangeHandler(selectedOutAtCountryCode);

            selectedInAtTimeZone = "America/New_York";
            selectedInAtTimeZoneValueChangeHandler(selectedInAtTimeZone);
            selectedOutAtTimeZone = "America/New_York";
            selectedOutAtTimeZoneValueChangeHandler(selectedOutAtTimeZone);


            // --------------------------------------------------------------------
            // Attempt to load the punch, if necessary.
            // --------------------------------------------------------------------

            if (Id.HasValue)
            {
                punch = await punchService.GetPunchByIdAsync(Id.Value);

                if (punch.InAt.ToString("hh").Contains("0") && punch.InAt.ToString("hh").IndexOf("0") == 0)
                    inAtHour = punch.InAt.ToString("hh").Replace("0", "");
                else
                    inAtHour = punch.InAt.ToString("hh");
                inAtMinute = punch.InAt.ToString("mm");
                inAtMeridian = punch.InAt.ToString("tt").ToUpper();

                if (punch.OutAt.HasValue)
                {
                    // Allow editing the punch out.
                    hasPunchOut = true;

                    if (punch.OutAt.Value.ToString("hh").Contains("0") && punch.OutAt.Value.ToString("hh").IndexOf("0") == 0)
                        outAtHour = punch.OutAt.Value.ToString("hh").Replace("0", "");
                    else
                        outAtHour = punch.OutAt.Value.ToString("hh");
                    outAtMinute = punch.OutAt.Value.ToString("mm");
                    outAtMeridian = punch.OutAt.Value.ToString("tt").ToUpper();
                }
                else
                {
                    // Set a default value.
                    punch.OutAt = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, 17, 0, 0);
                    selectedOutAtCountryCode = "US";
                    selectedOutAtCountryCodeValueChangeHandler(selectedOutAtCountryCode);
                    selectedOutAtTimeZone = "America/New_York";
                    selectedOutAtTimeZoneValueChangeHandler(selectedOutAtTimeZone);
                }

                // Set the customer, job, and task based on the TaskId for this punch.
                await RefreshCustomers(punch.Task.Job.CustomerId);
                await RefreshJobs(punch.Task.JobId);
                await RefreshTasks(punch.TaskId);

                // Set the InAt time zone.
                var inAtTimeZone = zones.Where(x => x.Id == punch.InAtTimeZone).FirstOrDefault();
                selectedInAtCountryCode = inAtTimeZone.CountryCode;
                selectedInAtCountryCodeValueChangeHandler(selectedInAtCountryCode);
                selectedInAtTimeZone = inAtTimeZone.Id;
                selectedInAtTimeZoneValueChangeHandler(selectedInAtTimeZone);

                // Set the OutAt time zone.
                var outAtTimeZone = zones.Where(x => x.Id == punch.OutAtTimeZone).FirstOrDefault();
                selectedOutAtCountryCode = outAtTimeZone.CountryCode;
                selectedOutAtCountryCodeValueChangeHandler(selectedInAtCountryCode);
                selectedOutAtTimeZone = outAtTimeZone.Id;
                selectedOutAtTimeZoneValueChangeHandler(selectedOutAtTimeZone);

                // Set the user based on the UserId for this punch.
                await RefreshUsers();

                if (!string.IsNullOrEmpty(punch.LatitudeForInAt) && !string.IsNullOrEmpty(punch.LongitudeForInAt))
                {
                    center = new GoogleMapPosition() { Lat = double.Parse(punch.LatitudeForInAt), Lng = double.Parse(punch.LongitudeForInAt) };
                }
                else if (!string.IsNullOrEmpty(punch.LatitudeForOutAt) && !string.IsNullOrEmpty(punch.LongitudeForOutAt))
                {
                    center = new GoogleMapPosition() { Lat = double.Parse(punch.LatitudeForOutAt), Lng = double.Parse(punch.LongitudeForOutAt) };
                }

                if (sharedService.CurrentUser.CanViewAudits)
                    await RefreshAudits();
            }
            else
            {
                // Set the customer, job, and task by selecting defaults.
                await RefreshCustomers();

                // Set a default user.
                await RefreshUsers();
            }


            loading = false;
            await InvokeAsync(StateHasChanged);
        }

        private async void SelectedCustomerValueChangeHandler(int customerId)
        {
            selectedCustomerId = customerId;
            await RefreshJobs();
        }

        private async void SelectedJobValueChangeHandler(int jobId)
        {
            selectedJobId = jobId;
            await RefreshTasks();
        }

        private void SelectedTaskValueChangeHandler(int taskId)
        {
            selectedTaskId = taskId;

            // Update the punch.
            punch.TaskId = selectedTaskId;
        }

        private void SelectedUserValueChangeHandler(int userId)
        {
            selectedUserId = userId;

            // Update the punch.
            punch.UserId = selectedUserId;
        }

        private void selectedInAtCountryCodeValueChangeHandler(string country)
        {
            selectedInAtCountryCode = country;
            inAtZones = zones.Where(z => z.CountryCode == selectedInAtCountryCode).ToList();
        }

        private void selectedInAtTimeZoneValueChangeHandler(string timeZone)
        {
            selectedInAtTimeZone = timeZone;
            punch.InAtTimeZone = selectedInAtTimeZone;
        }

        private void selectedOutAtCountryCodeValueChangeHandler(string country)
        {
            selectedOutAtCountryCode = country;
            outAtZones = zones.Where(z => z.CountryCode == selectedOutAtCountryCode).ToList();
        }

        private void selectedOutAtTimeZoneValueChangeHandler(string timeZone)
        {
            selectedOutAtTimeZone = timeZone;
            punch.OutAtTimeZone = selectedOutAtTimeZone;
        }

        private async System.Threading.Tasks.Task RefreshUsers()
        {
            loadingUsers = true;

            var result = await userService.GetUsersAsync(excludeInactiveUsers: true);
            users = result.Item1;

            // Update the selected user.
            if (punch.UserId != 0)
                SelectedUserValueChangeHandler(punch.UserId);
            else
                SelectedUserValueChangeHandler(users.FirstOrDefault().Id);

            loadingUsers = false;
            await InvokeAsync(StateHasChanged);
        }

        private async System.Threading.Tasks.Task RefreshCustomers(int? customerId = null)
        {
            loadingCustomers = true;

            var result = await customerService.GetCustomersAsync(pageSize: 1000, sortBy: sharedService.CurrentUser.Organization.SortCustomersByColumn);
            customers = result.Item1;

            if (customerId.HasValue)
            {
                // Attempt to find the requested customer in the list.
                var exists = customers.Where(c => c.Id == customerId).Any();
                if (exists)
                    selectedCustomerId = customerId.Value;
            }
            else
            {
                // Set the default value.
                selectedCustomerId = customers.FirstOrDefault().Id;

                // Trigger refresh for jobs.
                await RefreshJobs();
            }

            loadingCustomers = false;
            await InvokeAsync(StateHasChanged);
        }

        private async System.Threading.Tasks.Task RefreshJobs(int? jobId = null)
        {
            loadingJobs = true;

            var result = await jobService.GetFilteredJobsAsync(filterCustomerIds: [selectedCustomerId], pageSize: 1000, sortBy: sharedService.CurrentUser.Organization.SortProjectsByColumn, excludeClosedStatus: true);
            jobs = result.Item1;

            if (jobId.HasValue)
            {
                // Attempt to find the requested job in the list.
                var exists = jobs.Where(j => j.Id == jobId).Any();
                if (exists)
                    selectedJobId = jobId.Value;
            }
            else
            {
                // Set the default value, if possible.
                if (jobs.Count != 0)
                {
                    selectedJobId = jobs.FirstOrDefault().Id;

                    // Trigger refresh for tasks.
                    await RefreshTasks();
                }
                else
                {
                    tasks.Clear();
                    loadingTasks = false;
                }
            }

            loadingJobs = false;
            await InvokeAsync(StateHasChanged);
        }

        private async System.Threading.Tasks.Task RefreshTasks(int? taskId = null)
        {
            loadingTasks = true;

            var result = await taskService.GetTasksAsync(selectedJobId, pageSize: 1000, sortBy: sharedService.CurrentUser.Organization.SortTasksByColumn);
            tasks = result.Item1;

            if (taskId.HasValue)
            {
                // Attempt to find the requested job in the list.
                var exists = tasks.Where(t => t.Id == taskId).Any();
                if (exists)
                    selectedTaskId = taskId.Value;
            }
            else
            {
                // Set the default value.
                selectedTaskId = tasks.FirstOrDefault().Id;

                // Update the punch.
                punch.TaskId = selectedTaskId;
            }

            loadingTasks = false;
            await InvokeAsync(StateHasChanged);
        }

        private async System.Threading.Tasks.Task RefreshAudits()
        {
            loadingAudits = true;

            var result = await auditService.GetPunchAuditsAsync(new DateTime(2021, 1, 1), new DateTime(2031, 1, 1), objectIds: new int[] { punch.Id });
            audits = result.Item1;

            loadingAudits = false;
            await InvokeAsync(StateHasChanged);
        }

        private async System.Threading.Tasks.Task SavePunch()
        {
            // Confirm that the user wants to make the change.
            var confirm = await dialogService.Confirm(
                "Are you sure you want to save this punch?",
                "Confirm",
                new ConfirmOptions() { Width = "600px", CancelButtonText = "Cancel", OkButtonText = "OK" });
            if (confirm != true)
                return;

            // Parse the new hour and minute.
            punch.InAt = DateTime.Parse($"{punch.InAt.Year}-{punch.InAt.Month}-{punch.InAt.Day} {inAtHour}:{inAtMinute} {inAtMeridian}");

            // Optionally parse the new hour and minute.
            if (hasPunchOut)
                punch.OutAt = DateTime.Parse($"{punch.OutAt.Value.Year}-{punch.OutAt.Value.Month}-{punch.OutAt.Value.Day} {outAtHour}:{outAtMinute} {outAtMeridian}");
            else
            {
                punch.OutAt = null;
                punch.OutAtTimeZone = null;
            }

            // Save the punch on the server and close the dialog.
            var response = await punchService.SavePunchAsync(punch);

            if (response.Item1)
            {
                dialogService.Close("punch.created");
            }
            else
            {
                //await dialogService.OpenAsync("Oops!", ds =>
                //    @< div >

                //        < p > @response.Item3 </ p >

                //        < div class="row">
                //        <div class="col-md-12">
                //            <button class="btn btn-default pull-right" type="button" @onclick="CloseDialog">OK</button>
                //        </div>
                //    </div>
                //</div>);
            }
        }

        private void ToggleInAtMerdian()
        {
            inAtMeridian = inAtMeridian == "AM" ? "PM" : "AM";
        }

        private void ToggleOutAtMerdian()
        {
            outAtMeridian = outAtMeridian == "AM" ? "PM" : "AM";
        }

        private async System.Threading.Tasks.Task DeletePunch()
        {
            // Confirm that the user wants to make the change.
            var confirm = await dialogService.Confirm(
                "Are you sure you want to delete this punch?",
                "Confirm",
                new ConfirmOptions() { Width = "600px", CancelButtonText = "Cancel", OkButtonText = "OK" });
            if (confirm != true)
                return;

            working = true;
            await InvokeAsync(StateHasChanged);

            // Delete the punch on the server and close the dialog.
            var result = await punchService.DeletePunchAsync(punch.Id);
            if (result)
                dialogService.Close("punch.deleted");
            else
            {
                working = false;
                await InvokeAsync(StateHasChanged);
            }
        }

        private void SelectAudit(Audit audit)
        {
            selectedAudit = audit;

            if (!string.IsNullOrEmpty(audit.Before))
                selectedBefore = JsonSerializer.Deserialize<Punch>(audit.Before);
            else
                selectedBefore = null;

            if (!string.IsNullOrEmpty(audit.After))
                selectedAfter = JsonSerializer.Deserialize<Punch>(audit.After);
            else
                selectedAfter = null;
        }

        private void CloseDialog(MouseEventArgs e)
        {
            dialogService.Close(false);
        }
    }
}
