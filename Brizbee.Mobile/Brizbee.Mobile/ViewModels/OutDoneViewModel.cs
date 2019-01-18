using Brizbee.Mobile.Views;
using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Input;
using Xamarin.Forms;

namespace Brizbee.Mobile.ViewModels
{
    public class OutDoneViewModel : BaseViewModel
    {
        public Page Page { get; set; }
        public ICommand ExitCommand { get; }
        public ICommand MoreCommand { get; }

        private INavigation nav = Application.Current.MainPage.Navigation;

        public OutDoneViewModel()
        {
            Title = "All Done";
            ExitCommand = new Command(async () => await Exit());
            MoreCommand = new Command(async () => await More());
        }

        private async System.Threading.Tasks.Task Exit()
        {
            await nav.PopToRootAsync();
        }

        private async System.Threading.Tasks.Task More()
        {
            await nav.PopAsync();
            await nav.PushAsync(new StatusPage());
        }
    }
}
