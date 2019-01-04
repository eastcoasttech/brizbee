﻿using System;
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

            //MainPage = new MainPage();
            MainPage = new NavigationPage(new LoginPage());
            //{
            //    BarBackgroundColor = Color.DarkGray
            //};
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
