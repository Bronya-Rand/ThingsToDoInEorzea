using System;
using System.Collections.Generic;
using ThingsToDoInEorzea.Conditions.Modifiers;
using ThingsToDoInEorzea.Conditions.Primary;
using ThingsToDoInEorzea.Services;

namespace ThingsToDoInEorzea.Windows.Components
{
    /// <summary>
    /// Provides display names for primary and modifier condition types.
    /// </summary>
    internal static class ObjectiveConditionDisplay
    {
        private static readonly Dictionary<Type, string> PrimaryKeys = new()
        {
            [typeof(ItemCountCondition)] = "ui.condition.item_count",
            [typeof(DutyClearCondition)] = "ui.condition.duty_clear",
            [typeof(CustomTextCondition)] = "ui.condition.custom_text",
            [typeof(FateClearCondition)] = "ui.condition.fate_clear"
        };
        private static readonly Dictionary<Type, string> ModifierKeys = new()
        {
            [typeof(JobCondition)] = "ui.condition.job",
            [typeof(TimerCondition)] = "ui.condition.timer",
            [typeof(WipeCondition)] = "ui.condition.wipe",
            [typeof(DutySettingsCondition)] = "ui.condition.duty_settings",
            [typeof(DeathCondition)] = "ui.condition.death"
        };
        public static string GetPrimaryName(Type t, LocalizationService? loc = null) =>
            PrimaryKeys.TryGetValue(t, out var key) ? (loc ?? new LocalizationService()).T(key) : t.Name;
        public static string GetModifierName(Type t, LocalizationService? loc = null) =>
            ModifierKeys.TryGetValue(t, out var key) ? (loc ?? new LocalizationService()).T(key) : t.Name;
    }
}
