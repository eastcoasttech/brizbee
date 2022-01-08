using Brizbee.Api.Serialization;
using Brizbee.Core.Models;
using Brizbee.Core.Security;
using Brizbee.Core.Serialization;
using Microsoft.OData.Edm;
using Microsoft.OData.ModelBuilder;

namespace Brizbee.Api
{
    public class BrizbeeEntityDataModel
    {
        public static IEdmModel GetEntityDataModel()
        {
            var builder = new ODataConventionModelBuilder();

            builder.EntitySet<Customer>("Customers");
            builder.EntitySet<Job>("Jobs");
            builder.EntitySet<Organization>("Organizations");
            builder.EntitySet<Punch>("Punches");
            builder.EntitySet<Rate>("Rates");
            builder.EntitySet<QuickBooksDesktopExport>("QuickBooksDesktopExports");
            builder.EntitySet<Brizbee.Core.Models.Task>("Tasks");
            builder.EntitySet<TaskTemplate>("TaskTemplates");
            builder.EntitySet<TimesheetEntry>("TimesheetEntries");
            builder.EntitySet<User>("Users");

            // Collection Function - Jobs/Open
            builder.EntityType<Job>()
                .Collection
                .Function("Open")
                .ReturnsCollectionFromEntitySet<Job>("Jobs");

            // Collection Function - Punches/Current
            builder.EntityType<Punch>()
                .Collection
                .Function("Current")
                .ReturnsFromEntitySet<Punch>("Punches");

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
            builder.EntityType<Brizbee.Core.Models.Task>()
                .Collection
                .Action("NextNumber");

            // Collection Function - Organizations/Countries
            var countries = builder.EntityType<Organization>()
                .Collection
                .Function("Countries");
            countries.ReturnsCollection<Country>();

            // Collection Function - Organizations/TimeZones
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

            // Collection Function - Tasks/Search
            var tasksSearch = builder.EntityType<Brizbee.Core.Models.Task>()
                .Collection
                .Function("Search")
                .Returns<string>();
            tasksSearch.Parameter<string>("Number");

            // Collection Function - Tasks/ForPunches
            var tasksForPunches = builder.EntityType<Brizbee.Core.Models.Task>()
                .Collection
                .Function("ForPunches");
            tasksForPunches.Parameter<string>("InAt");
            tasksForPunches.Parameter<string>("OutAt");
            tasksForPunches.ReturnsCollectionFromEntitySet<Brizbee.Core.Models.Task>("Tasks");

            // Collection Function - Rates/BasePayrollRatesForPunches
            var basePayrollRates = builder.EntityType<Rate>()
                .Collection
                .Function("BasePayrollRatesForPunches");
            basePayrollRates.Parameter<DateTimeOffset>("InAt");
            basePayrollRates.Parameter<DateTimeOffset>("OutAt");
            basePayrollRates.ReturnsCollectionFromEntitySet<Rate>("Rates");

            // Collection Function - Rates/BaseServiceRatesForPunches
            var baseServiceRates = builder.EntityType<Rate>()
                .Collection
                .Function("BaseServiceRatesForPunches");
            baseServiceRates.Parameter<DateTimeOffset>("InAt");
            baseServiceRates.Parameter<DateTimeOffset>("OutAt");
            baseServiceRates.ReturnsCollectionFromEntitySet<Rate>("Rates");

            // Collection Function - Rates/AlternatePayrollRatesForPunches
            var alternatePayrollRates = builder.EntityType<Rate>()
                .Collection
                .Function("AlternatePayrollRatesForPunches");
            alternatePayrollRates.Parameter<DateTimeOffset>("InAt");
            alternatePayrollRates.Parameter<DateTimeOffset>("OutAt");
            alternatePayrollRates.ReturnsCollectionFromEntitySet<Rate>("Rates");

            // Collection Function - Rates/AlternateServiceRatesForPunches
            var alternateServiceRates = builder.EntityType<Rate>()
                .Collection
                .Function("AlternateServiceRatesForPunches");
            alternateServiceRates.Parameter<DateTimeOffset>("InAt");
            alternateServiceRates.Parameter<DateTimeOffset>("OutAt");
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
            var changePassword = builder.EntityType<User>()
                .Action("ChangePassword");
            changePassword.Parameter<string>("Password");

            return builder.GetEdmModel();
        }
    }
}
