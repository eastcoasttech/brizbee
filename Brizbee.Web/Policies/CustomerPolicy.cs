﻿using Brizbee.Common.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Brizbee.Policies
{
    public class CustomerPolicy
    {
        public static Boolean CanCreate(Customer customer, User currentUser)
        {
            return true;
        }

        public static Boolean CanUpdate(Customer customer, User currentUser)
        {
            return true;
        }
    }
}