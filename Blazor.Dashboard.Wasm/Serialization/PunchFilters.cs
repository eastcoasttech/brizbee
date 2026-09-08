
using Brizbee.Dashboard.Wasm.Models;
using System.Collections.Generic;

namespace Brizbee.Dashboard.Wasm.Serialization
{
    public class PunchFilters
    {
        public HashSet<User> Users { get; set; }

        public HashSet<Brizbee.Dashboard.Wasm.Models.Task> Tasks { get; set; }

        public HashSet<Job> Projects { get; set; }

        public HashSet<Customer> Customers { get; set; }

        public int Count
        {
            get
            {
                return Users.Count + Tasks.Count + Projects.Count + Customers.Count;
            }
        }
    }
}
