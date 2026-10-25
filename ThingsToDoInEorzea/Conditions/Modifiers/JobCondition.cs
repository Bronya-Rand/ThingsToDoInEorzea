using ThingsToDoInEorzea.Conditions.Helpers;
using ThingsToDoInEorzea.Models;
using ThingsToDoInEorzea.Services;
using ThingsToDoInEorzea.Services.Browser;

namespace ThingsToDoInEorzea.Conditions.Modifiers
{
    /// <summary>
    /// Represents a condition that checks whether the player is a specific job/class when doing a duty or fate.
    /// </summary>
    public sealed class JobCondition : ModifierConditionBase
    {
        private const uint AddonRowId = 6927;

        public uint ClassJobId { get; set; }

        public override ClauseSectionKind Section => ClauseSectionKind.Requirements;
        public override bool CheckOnStart => true;

        public override bool Matches(GameEvent evt, GameDataServices gameData, Configuration configuration)
        {
            switch (evt.Type)
            {
                case GameEventType.DutyStarted:
                case GameEventType.DutyCompleted:
                case GameEventType.FateCompleted:
                {
                    if (evt.ClassJobId == null) return false;
                    var jobId = evt.ClassJobId.Value;
                    var effectiveTargetJobId = gameData.JobBrowserService.GetEffectiveJobId(ClassJobId);
                    LastKnownResult = JobBrowserService.MatchesJob(jobId, effectiveTargetJobId);
                    return LastKnownResult.Value;
                }
                default:
                    return base.Matches(evt);
            }
        }

        public override bool IsValid() => ClassJobId > 0;

        public override ClauseSegment? DescribeSegment(GameDataServices gameData, Configuration configuration)
        {
            if (!IsValid()) return null;

            var effectiveTargetJobId = gameData.JobBrowserService.GetEffectiveJobId(ClassJobId);
            return ClauseSegment.ValueOnly(EvalSource.FromAddon(AddonRowId, [effectiveTargetJobId]));
        }

        public override string DescribeFailure(GameEvent evt, GameDataServices gameData)
        {
            if (evt.ClassJobId == null) return "No job information available.";

            var actualJobName = gameData.JobBrowserService.GetJobName(evt.ClassJobId.Value);
            var effectiveTargetJobId = gameData.JobBrowserService.GetEffectiveJobId(ClassJobId);
            var requiredJobName = gameData.JobBrowserService.GetJobName(effectiveTargetJobId);
            return $"Entered as {actualJobName}, but required {requiredJobName}.";
        }
    }
}
