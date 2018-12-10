using Brizbee.Common.Models;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Text;
using Xamarin.Forms;

namespace Brizbee.Mobile.ViewModels
{
    public class InConfirmViewModel : BaseViewModel
    {
        public bool IsEnabled { get; set; }
        public string JobNumberAndName { get; set; }
        public string CustomerNumberAndName { get; set; }
        public string TaskNumberAndName { get; set; }

        private RestClient client = Application.Current.Properties["RestClient"] as RestClient;

        public InConfirmViewModel()
        {
            IsEnabled = true;
            Title = "Confirm Task Number";

            var task = Application.Current.Properties["SelectedTask"] as Task;
            JobNumberAndName = string.Format("{0} - {1}", task.Job.Number, task.Job.Name);
            CustomerNumberAndName = string.Format("{0} - {1}", task.Job.Customer.Number, task.Job.Customer.Name);
            TaskNumberAndName = string.Format("{0} - {1}", task.Number, task.Name);
        }
    }
}
