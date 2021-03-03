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

        public event Action OnChange;

        private void NotifyDataChanged() => OnChange?.Invoke();

        public void Reset()
        {
            // Clear variables
            _authExpiration = "";
            _authToken = "";
            _authUserId = 0;
            _currentUser = null;
        }
    }
}
