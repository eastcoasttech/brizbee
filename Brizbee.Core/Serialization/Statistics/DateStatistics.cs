namespace Brizbee.Core.Serialization.Statistics
{
    public class DateStatistics
    {
        public DateTime Date { get; set; }

        public decimal MinutesCount { get; set; }

        public decimal HoursCount
        {
            get
            {
                return Math.Round(MinutesCount / 60.0m, 1);
            }
        }
    }
}
