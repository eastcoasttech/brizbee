using Brizbee.Common.Models;
using Brizbee.Common.Security;
using Brizbee.Common.Serialization;
using Brizbee.Web.Filters;
using Brizbee.Web.Serialization;
using Microsoft.AspNet.OData.Builder;
using Microsoft.AspNet.OData.Extensions;
using System;
using System.Linq;
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
            builder.EntitySet<Rate>("Rates");
            builder.EntitySet<QuickBooksDesktopExport>("QuickBooksDesktopExports");
            builder.EntitySet<QuickBooksOnlineExport>("QuickBooksOnlineExports");
            builder.EntitySet<Task>("Tasks");
            builder.EntitySet<TaskTemplate>("TaskTemplates");
            builder.EntitySet<TimesheetEntry>("TimesheetEntries");
            builder.EntitySet<User>("Users");

            // Collection Function - Punches/Current
            builder.EntityType<Punch>()
                .Collection
                .Function("Current")
                .ReturnsFromEntitySet<Punch>("Punches");

            // Member Function - Commit/Export
            var export = builder.EntityType<Commit>()
                .Function("Export")
                .Returns<string>();
            export.Parameter<string>("Delimiter");

            // Collection Action - Users/Authenticate
            var authenticate = builder.EntityType<User>()
                .Collection
                .Action("Authenticate");
            authenticate.Parameter<Session>("Session");
            authenticate.Returns<Credential>();

            // Collection Action - Customers/NextNumber
            builder.EntityType<Customer>()
                .Collection
                .Action("NextNumber");

            // Collection Action - Jobs/NextNumber
            builder.EntityType<Job>()
                .Collection
                .Action("NextNumber");

            // Collection Action - Tasks/NextNumber
            builder.EntityType<Task>()
                .Collection
                .Action("NextNumber");

            // Collection Action - Organizations/Countries
            var countries = builder.EntityType<Organization>()
                .Collection
                .Function("Countries");
            countries.ReturnsCollection<Country>();

            // Collection Action - Organizations/TimeZones
            var timeZones = builder.EntityType<Organization>()
                .Collection
                .Function("TimeZones");
            timeZones.ReturnsCollection<IanaTimeZone>();

            // Collection Action - Users/Register
            var register = builder.EntityType<User>()
                .Collection
                .Action("Register");
            register.Parameter<Organization>("Organization");
            register.Parameter<User>("User");
            register.ReturnsFromEntitySet<User>("Users");

            // Collection Action - Punches/PunchIn
            var punchIn = builder.EntityType<Punch>()
                .Collection
                .Action("PunchIn")
                .ReturnsFromEntitySet<Punch>("Punches");
            punchIn.Parameter<int>("TaskId");
            punchIn.Parameter<string>("InAtTimeZone");
            punchIn.Parameter<string>("LatitudeForInAt");
            punchIn.Parameter<string>("LongitudeForInAt");
            punchIn.Parameter<string>("SourceHardware");
            punchIn.Parameter<string>("SourceOperatingSystem");
            punchIn.Parameter<string>("SourceOperatingSystemVersion");
            punchIn.Parameter<string>("SourceBrowser");
            punchIn.Parameter<string>("SourceBrowserVersion");

            // Collection Action - Punches/PunchOut
            var punchOut = builder.EntityType<Punch>()
                .Collection
                .Action("PunchOut")
                .ReturnsFromEntitySet<Punch>("Punches");
            punchOut.Parameter<string>("OutAtTimeZone");
            punchOut.Parameter<string>("LatitudeForOutAt");
            punchOut.Parameter<string>("LongitudeForOutAt");
            punchOut.Parameter<string>("SourceHardware");
            punchOut.Parameter<string>("SourceOperatingSystem");
            punchOut.Parameter<string>("SourceOperatingSystemVersion");
            punchOut.Parameter<string>("SourceBrowser");
            punchOut.Parameter<string>("SourceBrowserVersion");

            // Collection Function - Punches/Download
            var download = builder.EntityType<Punch>()
                .Collection
                .Function("Download")
                .Returns<string>();
            download.Parameter<int>("CommitId");

            // Collection Function - Tasks/ForPunches
            var tasksForPunches = builder.EntityType<Task>()
                .Collection
                .Function("ForPunches");
            tasksForPunches.Parameter<string>("InAt");
            tasksForPunches.Parameter<string>("OutAt");
            tasksForPunches.ReturnsCollectionFromEntitySet<Task>("Tasks");

            // Collection Action - TimesheetEntries/Add
            var add = builder.EntityType<TimesheetEntry>()
                .Collection
                .Action("Add")
                .ReturnsFromEntitySet<TimesheetEntry>("TimesheetEntries");
            add.Parameter<int>("TaskId");
            add.Parameter<int>("Minutes");
            add.Parameter<string>("EnteredAt");
            add.Parameter<string>("Notes");

            // Collection Function - Rates/BasePayrollRatesForPunches
            var basePayrollRates = builder.EntityType<Rate>()
                .Collection
                .Function("BasePayrollRatesForPunches");
            basePayrollRates.Parameter<string>("InAt");
            basePayrollRates.Parameter<string>("OutAt");
            basePayrollRates.ReturnsCollectionFromEntitySet<Rate>("Rates");

            // Collection Function - Rates/BaseServiceRatesForPunches
            var baseServiceRates = builder.EntityType<Rate>()
                .Collection
                .Function("BaseServiceRatesForPunches");
            baseServiceRates.Parameter<string>("InAt");
            baseServiceRates.Parameter<string>("OutAt");
            baseServiceRates.ReturnsCollectionFromEntitySet<Rate>("Rates");

            // Collection Function - Rates/AlternatePayrollRatesForPunches
            var alternatePayrollRates = builder.EntityType<Rate>()
                .Collection
                .Function("AlternatePayrollRatesForPunches");
            alternatePayrollRates.Parameter<string>("InAt");
            alternatePayrollRates.Parameter<string>("OutAt");
            alternatePayrollRates.ReturnsCollectionFromEntitySet<Rate>("Rates");

            // Collection Function - Rates/AlternateServiceRatesForPunches
            var alternateServiceRates = builder.EntityType<Rate>()
                .Collection
                .Function("AlternateServiceRatesForPunches");
            alternateServiceRates.Parameter<string>("InAt");
            alternateServiceRates.Parameter<string>("OutAt");
            alternateServiceRates.ReturnsCollectionFromEntitySet<Rate>("Rates");

            // Collection Action - Punches/Split
            var split = builder.EntityType<Punch>()
                .Collection
                .Action("Split");
            split.Parameter<string>("InAt");
            split.Parameter<string>("Minutes");
            split.Parameter<string>("OutAt");
            split.Parameter<string>("Time");
            split.Parameter<string>("Type");

            // Collection Action - Punches/SplitAtMidnight
            var splitMidnight = builder.EntityType<Punch>()
                .Collection
                .Action("SplitAtMidnight");
            splitMidnight.Parameter<string>("InAt");
            splitMidnight.Parameter<string>("OutAt");

            // Collection Action - Punches/PopulateRates
            var populate = builder.EntityType<Punch>()
                .Collection
                .Action("PopulateRates");
            populate.Parameter<PopulateRateOptions>("Options");
            populate.Returns<string>();

            // Member Action - User/ChangePassword
            ActionConfiguration changePassword = builder.EntityType<User>()
                .Action("ChangePassword");
            changePassword.Parameter<string>("Password");

            // Member Action - Commit/Undo
            ActionConfiguration undo = builder.EntityType<Commit>()
                .Action("Undo");

            config.MapODataServiceRoute("odata", "odata", builder.GetEdmModel());

            config.EnableDependencyInjection();

            config.EnsureInitialized();
        }
    }
}
