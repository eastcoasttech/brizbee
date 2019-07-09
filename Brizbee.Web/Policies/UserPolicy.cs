using Brizbee.Common.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Brizbee.Web.Policies
{
    public class UserPolicy
    {
        public static Boolean CanChangePassword(User user, User currentUser)
        {
            return true;
        }

        public static Boolean CanCreate(User user, User currentUser)
        {
            return true;
        }

        public static Boolean CanDelete(User user, User currentUser)
        {
            return true;
        }

        public static Boolean CanUpdate(User user, User currentUser)
        {
            return true;
        }
    }
}