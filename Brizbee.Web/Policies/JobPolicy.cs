using Brizbee.Common.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Brizbee.Policies
{
    public class JobPolicy
    {
        public static Boolean CanCreate(Job job, User currentUser)
        {
            return true;
        }

        public static Boolean CanDelete(Job job, User currentUser)
        {
            return true;
        }

        public static Boolean CanUpdate(Job job, User currentUser)
        {
            return true;
        }
    }
}