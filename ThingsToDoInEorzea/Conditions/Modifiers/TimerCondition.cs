using System;
using System.Text.Json.Serialization;
using Dalamud.Game.ClientState.Conditions;
using ThingsToDoInEorzea.Conditions.Helpers;
using ThingsToDoInEorzea.Models;
using ThingsToDoInEorzea.Services;

namespace ThingsToDoInEorzea.Conditions.Modifiers
{
    /// <summary>
    /// Represents a condition that checks whether the current duty is completed within a specified time limit.
    /// </summary>
    public sealed class TimerCondition : ModifierConditionBase
    {
        private const uint AddonRowId = 603;

        public TimeSpan MaxDuration { get; set; }
        public override ClauseSectionKind Section => ClauseSectionKind.Performance;

        private DateTime? startTime;
        private static bool InDuty => DalamudServices.Condition[ConditionFlag.BoundByDuty];
        private TimeSpan lastElapsedTime;

        [JsonIgnore] public TimeSpan Elapsed => startTime.HasValue ? DateTime.UtcNow - startTime.Value : TimeSpan.Zero;
        [JsonIgnore] public bool IsRunning => startTime.HasValue;
        [JsonIgnore] public TimeSpan Remaining => MaxDuration > Elapsed ? (MaxDuration - Elapsed) : TimeSpan.Zero;
        [JsonIgnore]
        public override float? Progress =>
            IsRunning && MaxDuration > TimeSpan.Zero
            ? (float?)Math.Clamp((Elapsed.TotalSeconds / MaxDuration.TotalSeconds), 0f, 1f)
            : null;
        [JsonIgnore] public bool IsExpired => IsRunning && Elapsed > MaxDuration;

        [JsonIgnore] public override bool IsFailed => (lastKnownResult.HasValue && !lastKnownResult.Value) || IsExpired;

        protected override void OnDutyStarted(GameEvent evt)
        {
            startTime = evt.Timestamp;
            lastKnownResult = null;
            lastElapsedTime = TimeSpan.Zero;
        }
        public override bool Matches(GameEvent evt)
        {
            switch (evt.Type)
            {
                case GameEventType.DutyCompleted:
                    {
                        if (!startTime.HasValue) return false;
                        lastElapsedTime = evt.Timestamp - startTime.Value;
                        startTime = null;
                        lastKnownResult = lastElapsedTime <= MaxDuration;
                        return lastKnownResult.Value;
                    }
                case GameEventType.DutyWiped:
                    startTime = null;
                    lastKnownResult = null;
                    return false;
                case GameEventType.DutyRecommenced:
                    if (InDuty)
                        startTime = evt.Timestamp;
                    return false;
                default:
                    if (IsExpired)
                        lastKnownResult = false;
                    return base.Matches(evt);
            }
        }
        public override bool IsValid() => MaxDuration > TimeSpan.Zero;

        public override ClauseSegment? DescribeSegment(GameDataServices gameData, Configuration configuration)
        {
            if (!IsValid()) return null;
            return ClauseSegment.ValueOnly(EvalSource.FromAddon(AddonRowId, [(int)MaxDuration.TotalMinutes]));
        }

        public override string DescribeFailure(GameEvent evt, GameDataServices gameData) =>
            $"Duty exceeded the maximum duration of {MaxDuration.TotalMinutes} minutes. Elapsed time: {lastElapsedTime:mm\\:ss}.";
    }
}
