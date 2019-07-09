using Brizbee.Common.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Brizbee.Web.Policies
{
    public class PunchPolicy
    {
        public static Boolean CanCreate(Punch punch, User currentUser)
        {
            return true;
        }

        public static Boolean CanDelete(Punch punch, User currentUser)
        {
            return true;
        }

        public static Boolean CanUpdate(Punch punch, User currentUser)
        {
            return true;
        }
    }
}