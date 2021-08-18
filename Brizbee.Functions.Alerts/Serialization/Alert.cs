namespace Brizbee.Functions.Alerts.Serialization
{
    public class Alert
    {
        public string Type { get; set; }

        public long Value { get; set; }

        public User User { get; set; }
    }
}
