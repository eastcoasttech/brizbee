namespace Brizbee.Core.Serialization.Statistics
{
    public class ProjectStatistics
    {
        public decimal MinutesCount { get; set; }

        public int WorkedDaysCount { get; set; }

        public int DurationDaysCount { get; set; }

        public List<TaskStatistics> TaskStatistics { get; set; } = new List<TaskStatistics>();

        public List<UserStatistics> UserStatistics { get; set; } = new List<UserStatistics>();

        public List<DateStatistics> DateStatistics { get; set; } = new List<DateStatistics>();
    }
}
