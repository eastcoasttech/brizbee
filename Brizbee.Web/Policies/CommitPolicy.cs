using Brizbee.Common.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Brizbee.Policies
{
    public class CommitPolicy
    {
        public static Boolean CanCreate(Commit commit, User currentUser)
        {
            return true;
        }
    }
}