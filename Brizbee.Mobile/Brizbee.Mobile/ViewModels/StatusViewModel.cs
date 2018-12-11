using Brizbee.Common.Models;
using System;
using System.Collections.Generic;
using System.Text;
using Xamarin.Forms;

namespace Brizbee.Mobile.ViewModels
{
    public class StatusViewModel : BaseViewModel
    {
        public string Hello { get; set; }
        public string Status { get; set; }

        public StatusViewModel()
        {
            var user = Application.Current.Properties["CurrentUser"] as User;
            Title = "Status";
            Hello = user.Name;
            Status = "PUNCHED OUT";
        }
    }
}
