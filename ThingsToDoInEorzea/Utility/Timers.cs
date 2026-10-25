using System;
using ThingsToDoInEorzea.Models;

namespace ThingsToDoInEorzea.Utility
{
    public static class Timers
    {
        private static DateTime GetLastDayOfWeek(DayOfWeek weekday, int hour)
        {
            var now = DateTime.UtcNow;
            // Calculate days since the last occurrence of the specified weekday
            var daysSince = ((int)now.DayOfWeek - (int)weekday + 7) % 7;
            var candidate = now.Date.AddDays(-daysSince).AddHours(hour);
            return now >= candidate ? candidate : candidate.AddDays(-7);
        }

        private static DateTime GetLastDailyReset(int hour)
        {
            var now = DateTime.UtcNow;
            var todayReset = now.Date.AddHours(hour);
            return now >= todayReset ? todayReset : todayReset.AddDays(-1);
        }

        public static DateTime GetLastDailyResetTime() =>
            GetLastDailyReset(15); // 15:00 UTC

        public static DateTime GetLastWeeklyResetTime() =>
            GetLastDayOfWeek(DayOfWeek.Tuesday, 8); // Tuesday 08:00 UTC

        public static bool NeedsReset(ResetInterval recurrence, DateTime lastResetAt)
            => recurrence switch
            {
                ResetInterval.Daily => lastResetAt < GetLastDailyResetTime(),
                ResetInterval.Weekly => lastResetAt < GetLastWeeklyResetTime(),
                _ => false,
            };
    }
}
