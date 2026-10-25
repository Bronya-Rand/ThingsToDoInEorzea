using System;
using Dalamud.Game.ClientState.Conditions;

namespace ThingsToDoInEorzea.Models
{
    public sealed class GameEvent
    {
        public required GameEventType Type { get; init; }
        public uint? TerritoryTypeId { get; init; }
        public uint? ContentFinderConditionId { get; init; }
        public uint? ClassJobId { get; init; }
        public uint? ItemId { get; init; }
        public bool? IsHq { get; init; }
        public int? Quantity { get; init; }

        #region Condition-related properties
        public ConditionFlag? ConditionFlag { get; init; }
        public bool? ConditionValue { get; init; }
        #endregion

        #region FATE-related properties
        public uint? FateRowId { get; init; }
        public uint? FateRewardMedal { get; init; }
        #endregion

        public DateTime Timestamp { get; init; }
    }
    public enum GameEventType
    {
        DutyStarted,
        DutyCompleted,
        DutyWiped,
        DutyRecommenced,
        DutyAbandon,
        InventoryChanged,
        PlayerDied,
        FateCompleted,
    }
}
