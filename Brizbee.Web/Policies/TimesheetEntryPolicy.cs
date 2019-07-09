using Brizbee.Common.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Brizbee.Web.Policies
{
    public class TimesheetEntryPolicy
    {
        public static Boolean CanCreate(TimesheetEntry timesheetEntry, User currentUser)
        {
            return true;
        }

        public static Boolean CanDelete(TimesheetEntry timesheetEntry, User currentUser)
        {
            return true;
        }

        public static Boolean CanUpdate(TimesheetEntry timesheetEntry, User currentUser)
        {
            return true;
        }
    }
}