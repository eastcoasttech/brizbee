using Brizbee.Common.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Brizbee.Web.Tests
{
    class TestTaskDbSet : TestDbSet<Task>
    {
        public override Task Find(params object[] keyValues)
        {
            return this.SingleOrDefault(obj => obj.Id == (int)keyValues.Single());
        }
    }
}
