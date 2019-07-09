using Brizbee.Common.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Brizbee.Web.Policies
{
    public class TaskTemplatePolicy
    {
        public static Boolean CanCreate(TaskTemplate taskTemplate, User currentUser)
        {
            return true;
        }

        public static Boolean CanDelete(TaskTemplate taskTemplate, User currentUser)
        {
            return true;
        }

        public static Boolean CanUpdate(TaskTemplate taskTemplate, User currentUser)
        {
            return true;
        }
    }
}