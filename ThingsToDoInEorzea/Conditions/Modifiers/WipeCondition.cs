using ThingsToDoInEorzea.Conditions.Helpers;
using ThingsToDoInEorzea.Models;
using ThingsToDoInEorzea.Services;

namespace ThingsToDoInEorzea.Conditions.Modifiers
{
    /// <summary>
    /// Represents a condition that checks whether the player has exceeded a specified number of wipes during a duty.
    /// </summary>
    public class WipeCondition : ModifierConditionBase
    {
        public int MaxAllowedWipes { get; set; } = 0;
        private int CurrentWipes { get; set; } = 0;

        public override ClauseSectionKind Section => ClauseSectionKind.Performance;

        protected override void OnDutyStarted(GameEvent evt)
        {
            CurrentWipes = 0;
            lastKnownResult = null;
        }
        public override bool Matches(GameEvent evt)
        {
            switch (evt.Type)
            {
                case GameEventType.DutyWiped:
                    CurrentWipes++;
                    if (CurrentWipes > MaxAllowedWipes)
                        lastKnownResult = false;
                    return CurrentWipes <= MaxAllowedWipes;
                case GameEventType.DutyCompleted:
                    lastKnownResult = CurrentWipes <= MaxAllowedWipes;
                    return lastKnownResult.Value;
                default:
                    return base.Matches(evt);
            }
        }
        public override bool IsValid() => MaxAllowedWipes >= 0;
        public override ClauseSegment? DescribeSegment(GameDataServices gameData, Configuration configuration)
        {
            return MaxAllowedWipes == 0
                ? ClauseSegment.ValueOnly(EvalSource.FromLocKey("clause.wipes_none"))
                : ClauseSegment.ValueOnly(EvalSource.FromLocKey("clause.wipes", CurrentWipes, MaxAllowedWipes));
        }
        public override string DescribeFailure(GameEvent evt, GameDataServices gameData)
        {
            var wipePlural = CurrentWipes == 1 ? "" : "s";
            var allowedPlural = MaxAllowedWipes == 1 ? "" : "s";
            return $"Wiped {CurrentWipes} time{wipePlural}. Allowed no more than {MaxAllowedWipes} wipe{allowedPlural}.";
        }
    }
}
