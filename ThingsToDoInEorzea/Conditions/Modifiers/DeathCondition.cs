using ThingsToDoInEorzea.Conditions.Helpers;
using ThingsToDoInEorzea.Models;
using ThingsToDoInEorzea.Services;

namespace ThingsToDoInEorzea.Conditions.Modifiers
{
    /// <summary>
    /// Represents a condition that checks whether the player has exceeded a maximum number of deaths during a duty.
    /// </summary>
    public class DeathCondition : ModifierConditionBase
    {
        private int currentDeaths;
        public int MaxDeaths { get; set; }

        public override ClauseSectionKind Section => ClauseSectionKind.Performance;
        public override bool IsFailed => currentDeaths > MaxDeaths;

        protected override void OnDutyStarted(GameEvent evt)
        {
            // Reset the death count and last known result when a duty starts
            currentDeaths = 0;
            LastKnownResult = null;
        }

        public override bool Matches(GameEvent evt)
        {
            switch (evt.Type)
            {
                case GameEventType.PlayerDied:
                    currentDeaths++;
                    if (currentDeaths > MaxDeaths)
                        LastKnownResult = false;
                    return currentDeaths <= MaxDeaths;
                case GameEventType.DutyCompleted:
                    LastKnownResult = currentDeaths <= MaxDeaths;
                    return LastKnownResult.Value;
                default:
                    return base.Matches(evt);
            }
        }

        public override bool IsValid() => MaxDeaths >= 0;

        public override ClauseSegment? DescribeSegment(GameDataServices gameData, Configuration configuration)
        {
            return MaxDeaths == 0
                       ? ClauseSegment.ValueOnly(EvalSource.FromLocKey("clause.deaths_none"))
                       : ClauseSegment.ValueOnly(EvalSource.FromLocKey("clause.deaths", currentDeaths, MaxDeaths));
        }

        public override string DescribeFailure(GameEvent evt, GameDataServices gameData)
        {
            var deathPlural = currentDeaths == 1 ? "" : "s";
            var allowedPlural = MaxDeaths == 1 ? "" : "s";
            return $"Died {currentDeaths} time{deathPlural}. Allowed no more than {MaxDeaths} death{allowedPlural}.";
        }
    }
}
