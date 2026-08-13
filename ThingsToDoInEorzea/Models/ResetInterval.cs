namespace ThingsToDoInEorzea.Models
{
    public enum ResetInterval
    {
        None,
        Daily,
        Weekly,
    }
    public static class ResetIntervalExtensions
    {
        public static string ToFriendlyString(this ResetInterval interval) => interval switch
        {
            ResetInterval.None => "None",
            ResetInterval.Daily => "Daily",
            ResetInterval.Weekly => "Weekly",
            _ => "Unknown"
        };
    }
}
