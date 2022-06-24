namespace Brizbee.Core.Serialization.Statistics
{
    public class ProjectStatistics
    {
        public decimal MinutesCount { get; set; }

        public List<TaskStatistics> TaskStatistics { get; set; } = new List<TaskStatistics>();
    }
}
