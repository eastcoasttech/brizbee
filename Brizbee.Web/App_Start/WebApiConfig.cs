using Brizbee.Common.Models;
using Brizbee.Common.Security;
using Brizbee.Filters;
using Microsoft.AspNet.OData.Builder;
using Microsoft.AspNet.OData.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Web.Http;
using System.Web.Http.Cors;

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

            // Set timezone
            config.SetTimeZoneInfo(TimeZoneInfo.Utc);

            // Custom authentication
            config.Filters.Add(new BrizbeeAuthorizeAttribute());

            // Custom exception handling
            config.Filters.Add(new CustomExceptionFilterAttribute());

            // Web API configuration and services
            ODataModelBuilder builder = new ODataConventionModelBuilder();
            config.Count().Filter().OrderBy().Expand().Select().MaxTop(null);
            builder.EntitySet<Customer>("Customers");
            builder.EntitySet<Commit>("Commits");
            builder.EntitySet<Job>("Jobs");
            builder.EntitySet<Organization>("Organizations");
            builder.EntitySet<Punch>("Punches");
            builder.EntitySet<Task>("Tasks");
            builder.EntitySet<TaskTemplate>("TaskTemplates");
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

            // Collection Action - Customer - NextNumber
            builder.EntityType<Customer>()
                .Collection
                .Action("NextNumber");

            // Collection Action - Job - NextNumber
            builder.EntityType<Job>()
                .Collection
                .Action("NextNumber");

            // Collection Action - Task - NextNumber
            builder.EntityType<Task>()
                .Collection
                .Action("NextNumber");

            // Collection Action - Organization - TimeZones
            var timeZones = builder.EntityType<Organization>()
                .Collection
                .Function("TimeZones");
            timeZones.ReturnsCollection<string>();

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
                .Action("PunchIn")
                .ReturnsFromEntitySet<Punch>("Punches");
            punchIn.Parameter<int>("TaskId");
            punchIn.Parameter<string>("SourceForInAt");
            punchIn.Parameter<string>("InAtTimeZone");

            // Collection Action - PunchOut
            var punchOut = builder.EntityType<Punch>()
                .Collection
                .Action("PunchOut")
                .ReturnsFromEntitySet<Punch>("Punches");
            punchOut.Parameter<string>("SourceForOutAt");
            punchOut.Parameter<string>("OutAtTimeZone");

            // Collection Action - Split
            var split = builder.EntityType<Punch>()
                .Collection
                .Action("Split");
            split.Parameter<string>("InAt");
            split.Parameter<string>("Minutes");
            split.Parameter<string>("OutAt");
            split.Parameter<string>("Time");
            split.Parameter<string>("Type");

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
