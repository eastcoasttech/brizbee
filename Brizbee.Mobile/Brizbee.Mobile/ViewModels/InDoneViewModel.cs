using Brizbee.Mobile.Views;
using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Input;
using Xamarin.Forms;

namespace Brizbee.Mobile.ViewModels
{
    public class InDoneViewModel : BaseViewModel
    {
        public ICommand ExitCommand { get; }
        public ICommand MoreCommand { get; }

        public InDoneViewModel()
        {
            Title = "All Done";
            ExitCommand = new Command(async () => await Exit());
            MoreCommand = new Command(async () => await More());
        }

        private async System.Threading.Tasks.Task Exit()
        {
            var nav = Application.Current.MainPage.Navigation;
            await nav.PopToRootAsync();
        }

        private async System.Threading.Tasks.Task More()
        {
            var nav = Application.Current.MainPage.Navigation;
            await nav.PopAsync();
            await nav.PushAsync(new StatusPage());
        }
    }
}
