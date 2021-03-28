using Brizbee.Common.Models;
using System;

namespace Brizbee.Dashboard.Services
{
    public class SharedService
    {
        private User _currentUser;
        private int _authUserId;
        private string _authExpiration;
        private string _authToken;
        private DateTime _rangeMin;
        private DateTime _rangeMax;

        public User CurrentUser
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

        public int AuthUserId
        {
            get
            {
                return _authUserId;
            }
            set
            {
                _authUserId = value;
                NotifyDataChanged();
            }
        }

        public string AuthExpiration
        {
            get
            {
                return _authExpiration;
            }
            set
            {
                _authExpiration = value;
                NotifyDataChanged();
            }
        }

        public string AuthToken
        {
            get
            {
                return _authToken;
            }
            set
            {
                _authToken = value;
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

        public event Action OnChange;

        private void NotifyDataChanged() => OnChange?.Invoke();

        public void Reset()
        {
            // Clear variables
            _authExpiration = "";
            _authToken = "";
            _authUserId = 0;
            _rangeMin = DateTime.Now;
            _rangeMax = DateTime.Now;
            _currentUser = null;
        }
    }
}
