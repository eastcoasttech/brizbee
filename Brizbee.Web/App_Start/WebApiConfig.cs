using Brizbee.Common.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Http;
using System.Web.Http.Cors;
using System.Web.OData.Builder;
using System.Web.OData.Extensions;

namespace Brizbee
{
    public static class WebApiConfig
    {
        public static void Register(HttpConfiguration config)
        {
            // Cross-Origin Resource Sharing
            var cors = new EnableCorsAttribute("*", "*", "*");
            config.EnableCors(cors);
            //config.EnableCors();

            // Web API routes
            config.MapHttpAttributeRoutes();

            config.Routes.MapHttpRoute(
                name: "DefaultApi",
                routeTemplate: "api/{controller}/{id}",
                defaults: new { id = RouteParameter.Optional }
            );

            // Web API configuration and services
            ODataModelBuilder builder = new ODataConventionModelBuilder();
            config.Count().Filter().OrderBy().Expand().Select().MaxTop(null);
            builder.EntitySet<Customer>("Customers");
            builder.EntitySet<Job>("Jobs");
            builder.EntitySet<Organization>("Organizations");
            builder.EntitySet<Punch>("Punches");
            builder.EntitySet<Task>("Tasks");
            builder.EntitySet<User>("Users");

            config.MapODataServiceRoute("odata", "odata", builder.GetEdmModel());
            
            config.EnableDependencyInjection();
            
            config.EnsureInitialized();
        }
    }
}
