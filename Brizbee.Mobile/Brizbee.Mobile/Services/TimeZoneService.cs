using Brizbee.Mobile.Models;
using Brizbee.Common.Serialization;
using NodaTime;
using NodaTime.TimeZones;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;

namespace Brizbee.Mobile.Services
{
    public class TimeZoneService
    {
        public static List<Country> GetCountries()
        {
            List<Country> countries = new List<Country>();

            CultureInfo[] cultures = CultureInfo.GetCultures(CultureTypes.SpecificCultures);
            
            foreach (var culture in cultures)
            {
                var region = new RegionInfo(culture.LCID);
                if (!countries.Where(c => c.Name == region.EnglishName).Any())
                {
                    countries.Add(new Country() { CountryCode = region.TwoLetterISORegionName, Name = region.EnglishName });
                }
            }
            
            return countries.OrderBy(c => c.Name).ToList();
        }

        public static List<IanaTimeZone> GetTimeZones(string countryCode)
        {
            var now = SystemClock.Instance.GetCurrentInstant();
            var tzdb = DateTimeZoneProviders.Tzdb;

            var zones = new List<IanaTimeZone>();
            var list =
                from location in TzdbDateTimeZoneSource.Default.ZoneLocations
                where string.IsNullOrEmpty(countryCode) ||
                      location.CountryCode.Equals(countryCode,
                                                  StringComparison.OrdinalIgnoreCase)
                let zoneId = location.ZoneId
                let tz = tzdb[zoneId]
                let offset = tz.GetZoneInterval(now).StandardOffset
                orderby offset, zoneId
                select new
                {
                    Id = zoneId
                    //,Name = string.Format("({0:+HH:mm}) {1}", offset, zoneId)
                };

            foreach (var zone in list)
            {
                zones.Add(new IanaTimeZone() { Id = zone.Id, CountryCode = countryCode });
            }

            return zones;
        }
    }
}
