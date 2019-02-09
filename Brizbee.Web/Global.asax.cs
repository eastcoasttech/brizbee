using Stripe;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Http;
using System.Web.Routing;

namespace Brizbee
{
    public class WebApiApplication : System.Web.HttpApplication
    {
        protected void Application_Start()
        {
            // Set your secret key: remember to change this to your live secret key in production
            // See your keys here: https://dashboard.stripe.com/account/apikeys
            StripeConfiguration.SetApiKey(""); // 
            
            GlobalConfiguration.Configuration.Formatters.JsonFormatter.SerializerSettings.ReferenceLoopHandling = Newtonsoft.Json.ReferenceLoopHandling.Ignore;
            GlobalConfiguration.Configuration.Formatters.Remove(GlobalConfiguration.Configuration.Formatters.XmlFormatter);

            GlobalConfiguration.Configure(WebApiConfig.Register);
        }
    }
}
