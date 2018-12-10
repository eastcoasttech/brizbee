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
            Hello = string.Format("Hello {0}", user.Name);
            Status = "You are punched out";
        }
    }
}
