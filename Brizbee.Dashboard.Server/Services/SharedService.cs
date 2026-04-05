using Brizbee.Core.Models;
using Brizbee.Dashboard.Server.Serialization;
using Microsoft.JSInterop;
using NodaTime;

namespace Brizbee.Dashboard.Server.Services
{
    public class SharedService(IJSRuntime JSRuntime)
    {
        private User? _currentUser;
        private string? _token;
        private DateTime _rangeMin;
        private DateTime _rangeMax;
        private PunchFilters? _punchFilters;

        public User? CurrentUser
        {
            get
            {
                return _currentUser;
            }
            set
            {
                _currentUser = value;
                NotifyDataChanged();
            }
        }

        public string Token
        {
            get
            {
                return _token;
            }
            set
            {
                _token = value;
                NotifyDataChanged();
            }
        }

        public DateTime RangeMin
        {
            get
            {
                return _rangeMin == DateTime.MinValue ? new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, 0, 0, 0) : _rangeMin;
            }
            set
            {
                _rangeMin = value;
                NotifyDataChanged();
            }
        }

        public DateTime RangeMax
        {
            get
            {
                return _rangeMax == DateTime.MinValue ? new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, 23, 59, 59) : _rangeMax;
            }
            set
            {
                _rangeMax = value;
                NotifyDataChanged();
            }
        }

        public PunchFilters PunchFilters
        {
            get
            {
                if (_punchFilters == null)
                {
                    _punchFilters = new PunchFilters()
                    {
                        Users = new HashSet<User>(),
                        Tasks = new HashSet<Core.Models.Task>(),
                        Projects = new HashSet<Job>(),
                        Customers = new HashSet<Customer>()
                    };
                    NotifyDataChanged();
                    return _punchFilters;
                }
                else
                    return _punchFilters;
            }
            set
            {
                _punchFilters = value;
                NotifyDataChanged();
            }
        }

        public Core.Models.Task? AttemptedTask { get; set; }

        public event Action OnChange;

        private void NotifyDataChanged() => OnChange?.Invoke();

        public async System.Threading.Tasks.Task ResetAsync()
        {
            _token = null;
            _currentUser = null;
            _punchFilters = null;

            // Reset the date range to local time if we can retrieve
            // the time zone from the browser.
            var timeZoneId = await JSRuntime.InvokeAsync<string>("getTimeZoneId");

            if (!string.IsNullOrEmpty(timeZoneId))
            {
                var timeZone = DateTimeZoneProviders.Tzdb.GetZoneOrNull(timeZoneId);

                if (timeZone == null)
                {
                    return;
                }

                var nowInstant = SystemClock.Instance.GetCurrentInstant();
                var nowLocal = nowInstant.InZone(timeZone);
                var nowDateTime = nowLocal.LocalDateTime.ToDateTimeUnspecified();

                _rangeMin = nowDateTime;
                _rangeMax = nowDateTime;
            }
        }
    }
}
