using Brizbee.Core.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Radzen;

namespace Brizbee.Dashboard.Server.Components.Pages
{
    public partial class ProjectDialog : ComponentBase
    {
        [Parameter] public int? Id { get; set; }
        [Parameter] public int? CustomerId { get; set; }

        private bool working = false;
        private bool loading = true;
        private Job project = new Job();
        private Customer customer = new Customer();
        private string selectedStatus;
        private string selectedTaxability;
        private int selectedTaskTemplateId;
        private List<TaskTemplate> taskTemplates = new List<TaskTemplate>(0);

        protected override async System.Threading.Tasks.Task OnInitializedAsync()
        {
            // Set the defaults.
            selectedStatus = "Open";
            SelectedStatusValueChangeHandler(selectedStatus);

            // --------------------------------------------------------------------
            // Load the parent customer.
            // --------------------------------------------------------------------

            customer = await customerService.GetCustomerByIdAsync(CustomerId.Value);

            // --------------------------------------------------------------------
            // Attempt to load the project, if necessary.
            // --------------------------------------------------------------------

            if (Id.HasValue)
            {
                project = await projectService.GetJobByIdAsync(Id.Value);

                // Set the status.
                selectedStatus = project.Status;
                SelectedStatusValueChangeHandler(selectedStatus);

                // Set the taxability.
                selectedTaxability = project.Taxability;
                SelectedTaxabilityValueChangeHandler(selectedTaxability);
            }
            else
            {
                project.Number = await projectService.GetNextNumberAsync();

                // Set the taxability.
                selectedTaxability = "";
                SelectedTaxabilityValueChangeHandler(selectedTaxability);

                await RefreshTaskTemplates();

                // Select the default task template if there are any.
                if (taskTemplates.Any())
                    SelectedTaskTemplateIdValueChangeHandler(taskTemplates.FirstOrDefault().Id);
            }

            loading = false;
        }

        private async System.Threading.Tasks.Task SaveProject()
        {
            // Confirm that the user wants to make the change.
            var confirm = await dialogService.Confirm(
                "Are you sure you want to save this project?",
                "Confirm",
                new ConfirmOptions() { Width = "600px", CancelButtonText = "Cancel", OkButtonText = "OK" });
            if (confirm != true)
                return;

            working = true;
            StateHasChanged();

            // Set the cutomer id.
            project.CustomerId = CustomerId.Value;

            // Save the project on the server and close the dialog.
            await projectService.SaveJobAsync(project);
            dialogService.Close("project.created");
        }

        private async System.Threading.Tasks.Task DeleteProject()
        {
            // Confirm that the user wants to make the change.
            var confirm = await dialogService.Confirm(
                "Are you sure you want to delete this project?",
                "Confirm",
                new ConfirmOptions() { Width = "600px", CancelButtonText = "Cancel", OkButtonText = "OK" });
            if (confirm != true)
                return;

            working = true;
            StateHasChanged();

            // Delete the project on the server and close the dialog.
            var result = await projectService.DeleteJobAsync(project.Id);
            if (result)
                dialogService.Close("project.deleted");
            else
                working = false;
        }

        private async System.Threading.Tasks.Task RefreshTaskTemplates()
        {
            // Refresh the list of task templates.
            var result = await taskTemplateService.GetTaskTemplatesAsync();

            taskTemplates = result.Item1;
        }

        private void CloseDialog(MouseEventArgs e)
        {
            dialogService.Close(false);
        }

        private void SelectedStatusValueChangeHandler(string status)
        {
            selectedStatus = status;

            // Update the project.
            project.Status = selectedStatus;
        }

        private void SelectedTaxabilityValueChangeHandler(string taxability)
        {
            selectedTaxability = taxability;

            // Update the project.
            project.Taxability = selectedTaxability;
        }

        private void SelectedTaskTemplateIdValueChangeHandler(int taskTemplateId)
        {
            selectedTaskTemplateId = taskTemplateId;

            // Update the project.
            project.TaskTemplateId = selectedTaskTemplateId;
        }

        private void RefreshQuickBooksCustomerJob(string name)
        {
            if (Id.HasValue)
                return;

            project.QuickBooksCustomerJob = $"{customer.Name}:{project.Number} - {name}";
        }
    }
}
