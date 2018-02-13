using Brizbee.Common.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Brizbee.Policies
{
    public class TaskPolicy
    {
        public static Boolean CanCreate(Task task, User currentUser)
        {
            return true;
        }

        public static Boolean CanDelete(Task task, User currentUser)
        {
            return true;
        }

        public static Boolean CanUpdate(Task task, User currentUser)
        {
            return true;
        }
    }
}