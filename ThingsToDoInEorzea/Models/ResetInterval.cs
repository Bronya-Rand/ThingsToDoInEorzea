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
        extension(ResetInterval interval)
        {
            public string ToFriendlyString() => interval switch
            {
                ResetInterval.None => "None",
                ResetInterval.Daily => "Daily",
                ResetInterval.Weekly => "Weekly",
                _ => "Unknown"
            };
        }
    }
}
