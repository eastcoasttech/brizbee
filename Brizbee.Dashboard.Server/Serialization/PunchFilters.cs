using Brizbee.Core.Models;

namespace Brizbee.Dashboard.Server.Serialization
{
    public class PunchFilters
    {
        public HashSet<User> Users { get; set; }

        public HashSet<Core.Models.Task> Tasks { get; set; }

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
