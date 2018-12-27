using RestSharp;
using System;
using System.Collections.Generic;
using System.Text;
using Xamarin.Forms;

namespace Brizbee.Mobile.ViewModels
{
    public class OutConfirmViewModel : BaseViewModel
    {
        public bool IsEnabled { get; set; }

        private RestClient client = Application.Current.Properties["RestClient"] as RestClient;

        public OutConfirmViewModel()
        {
            IsEnabled = true;
            Title = "Confirm Punch Out";
        }
    }
}
