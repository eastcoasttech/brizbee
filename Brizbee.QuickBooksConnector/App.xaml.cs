using Hellang.MessageBus;
using RestSharp;
using System;
using System.Windows;
using System.Windows.Threading;

namespace Brizbee.QuickBooksConnector
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public App()
        {
            var client = new RestClient("https://brizbeeweb.azurewebsites.net/");
            Application.Current.Properties["Client"] = client;

            // Initialize MessageBus Using Dispatcher
            Action<Action> uiThreadMarshaller =
                action => Dispatcher.Invoke(DispatcherPriority.Normal, action);
            Application.Current.Properties["MessageBus"] =
                new MessageBus(uiThreadMarshaller);
        }
    }
}
