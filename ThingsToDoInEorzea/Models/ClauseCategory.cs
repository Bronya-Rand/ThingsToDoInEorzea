using System;

namespace ThingsToDoInEorzea.Models
{
    public enum ClauseCategory
    {
        // Constraints known at duty entry (e.g., job restrictions)
        Requirements,
        // Constraints that can only be resolved over the course of the duty
        // (e.g., time limit, wipe limit, death limit)
        Performance,
        // Duty configuration constraints (IL, Echo, Party Composition, etc.)
        DutySettings,
    }
    public static class ClauseCategoryExtensions
    {
        public static string GetName(this ClauseCategory category)
        {
            return category switch
            {
                ClauseCategory.Requirements => "Requirements",
                ClauseCategory.Performance => "Performance",
                ClauseCategory.DutySettings => "Duty Settings",
                _ => throw new ArgumentOutOfRangeException(nameof(category), category, null),
            };
        }
    }
}
