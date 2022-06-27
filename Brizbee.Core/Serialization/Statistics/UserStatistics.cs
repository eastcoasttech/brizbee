namespace Brizbee.Core.Serialization.Statistics
{
    public class UserStatistics
    {
        public int UserId { get; set; }

        public string UserName { get; set; } = string.Empty;

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
