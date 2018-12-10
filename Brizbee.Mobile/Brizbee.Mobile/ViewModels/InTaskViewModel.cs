using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Input;

namespace Brizbee.Mobile.ViewModels
{
    public class InTaskViewModel : BaseViewModel
    {
        public ICommand ContinueCommand { get; }

        public InTaskViewModel()
        {
            Title = "Enter Task Number";
        }
    }
}
