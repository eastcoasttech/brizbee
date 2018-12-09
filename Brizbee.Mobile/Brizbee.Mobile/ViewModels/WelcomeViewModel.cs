using System;
using System.Collections.Generic;
using System.Text;

namespace Brizbee.Mobile.ViewModels
{
    public class WelcomeViewModel : BaseViewModel
    {
        public string Hello { get; set; }
        public string Status { get; set; }

        public WelcomeViewModel()
        {
            Title = "Welcome";
            Hello = "Hello Joshua";
            Status = "You are punched out";
        }
    }
}
