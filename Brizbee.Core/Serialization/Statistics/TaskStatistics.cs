namespace Brizbee.Core.Serialization.Statistics
{
    public class TaskStatistics
    {
        public int TaskId { get; set; }

        public string TaskNumber { get; set; } = string.Empty;
        
        public string TaskName { get; set; } = string.Empty;

        public decimal MinutesCount { get; set; }

        public decimal HoursCount
        {
            get
            {
                return Math.Round(MinutesCount / 60.0m, 1);
            }
        }

        public string TaskNumberWithName
        {
            get
            {
                return $"{TaskNumber} - {TaskName}";
            }
        }
    }
}
