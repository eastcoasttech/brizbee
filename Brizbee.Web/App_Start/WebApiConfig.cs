using Brizbee.Common.Models;
using Brizbee.Common.Security;
using Brizbee.Filters;
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

            // Web API routes
            config.MapHttpAttributeRoutes();

            config.Routes.MapHttpRoute(
                name: "DefaultApi",
                routeTemplate: "api/{controller}/{id}",
                defaults: new { id = RouteParameter.Optional }
            );

            // Custom authentication and exception handling
            config.Filters.Add(new BrizbeeAuthorizeAttribute());
            //config.Filters.Add(new HttpBasicAuthorizeAttribute());

            // Web API configuration and services
            ODataModelBuilder builder = new ODataConventionModelBuilder();
            config.Count().Filter().OrderBy().Expand().Select().MaxTop(null);
            builder.EntitySet<Customer>("Customers");
            builder.EntitySet<Commit>("Commits");
            builder.EntitySet<Job>("Jobs");
            builder.EntitySet<Organization>("Organizations");
            builder.EntitySet<Punch>("Punches");
            builder.EntitySet<Task>("Tasks");
            builder.EntitySet<User>("Users");

            // Collection Function - Current
            builder.EntityType<Punch>()
                .Collection
                .Function("Current")
                .ReturnsFromEntitySet<Punch>("Punches");

            // Collection Action - Authenticate
            var authenticate = builder.EntityType<User>()
                .Collection
                .Action("Authenticate");
            authenticate.Parameter<Session>("Session");
            authenticate.Returns<Credential>();
            
            // Collection Action - Register
            var register = builder.EntityType<User>()
                .Collection
                .Action("Register");
            register.Parameter<Organization>("Organization");
            register.Parameter<User>("User");
            register.ReturnsFromEntitySet<User>("Users");

            // Collection Action - PunchIn
            var punchIn = builder.EntityType<Punch>()
                .Collection
                .Action("PunchIn");
            punchIn.Parameter<int>("TaskId");

            // Collection Action - PunchOut
            var punchOut = builder.EntityType<Punch>()
                .Collection
                .Action("PunchOut");

            // Member Action - Change Password
            ActionConfiguration changePassword = builder.EntityType<User>().Action("ChangePassword");
            changePassword.Parameter<string>("Password");

            // Member Action - Undo
            ActionConfiguration undo = builder.EntityType<Commit>().Action("Undo");

            config.MapODataServiceRoute("odata", "odata", builder.GetEdmModel());
            
            config.EnableDependencyInjection();
            
            config.EnsureInitialized();
        }
    }
}
