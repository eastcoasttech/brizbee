﻿using Brizbee.Common.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Brizbee.Web.Tests
{
    class TestCustomerDbSet : TestDbSet<Customer>
    {
        public override Customer Find(params object[] keyValues)
        {
            return this.SingleOrDefault(obj => obj.Id == (int)keyValues.Single());
        }
    }
}
