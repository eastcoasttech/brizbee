//
//  Global.asax.cs
//  BRIZBEE API
//
//  Copyright (C) 2020 East Coast Technology Services, LLC
//
//  This file is part of the BRIZBEE API.
//
//  This program is free software: you can redistribute it and/or modify
//  it under the terms of the GNU Affero General Public License as
//  published by the Free Software Foundation, either version 3 of the
//  License, or (at your option) any later version.
//
//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//  GNU Affero General Public License for more details.
//
//  You should have received a copy of the GNU Affero General Public License
//  along with this program.  If not, see <https://www.gnu.org/licenses/>.
//

using Microsoft.ApplicationInsights.Extensibility;
using Stripe;
using System;
using System.Configuration;
using System.Diagnostics;
using System.Web.Http;

namespace Brizbee.Web
{
    public class WebApiApplication : System.Web.HttpApplication
    {
        protected void Application_Start()
        {
            // Configure Stripe key
            StripeConfiguration.ApiKey = ConfigurationManager.AppSettings["StripeSecretKey"].ToString();

            GlobalConfiguration.Configuration.Formatters.JsonFormatter.SerializerSettings.ReferenceLoopHandling = Newtonsoft.Json.ReferenceLoopHandling.Ignore;
            GlobalConfiguration.Configuration.Formatters.Remove(GlobalConfiguration.Configuration.Formatters.XmlFormatter);

            GlobalConfiguration.Configure(WebApiConfig.Register);

            // Configure Application Insights key
            try
            {
                TelemetryConfiguration.Active.ConnectionString = ConfigurationManager.AppSettings["ApplicationInsightsConnectionString"].ToString();
                //TelemetryConfiguration.Active.TelemetryChannel.DeveloperMode = true;
            }
            catch (ArgumentNullException ex)
            {
                Trace.TraceError(ex.ToString());
            }
            catch (ConfigurationErrorsException ex)
            {
                Trace.TraceError(ex.ToString());
            }
            catch (Exception ex)
            {
                Trace.TraceError(ex.ToString());
            }
        }
    }
}
