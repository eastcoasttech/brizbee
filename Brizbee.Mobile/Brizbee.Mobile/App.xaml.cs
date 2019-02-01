using System;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;
using Brizbee.Mobile.Views;
using RestSharp;

[assembly: XamlCompilation(XamlCompilationOptions.Compile)]
namespace Brizbee.Mobile
{
    public partial class App : Application
    {

        public App()
        {
            InitializeComponent();

            Current.Properties["RestClient"] = new RestClient("https://brizbeeweb.azurewebsites.net/");
            //Current.Properties["RestClient"] = new RestClient("http://localhost:54313/");

            //MainPage = new MainPage();
            MainPage = new NavigationPage(new LoginPage());
        }

        protected override void OnStart()
        {
            // Handle when your app starts
        }

        protected override void OnSleep()
        {
            // Handle when your app sleeps
        }

        protected override void OnResume()
        {
            // Handle when your app resumes
        }
    }
}
