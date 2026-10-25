using System.Collections.Generic;
using ThingsToDoInEorzea.Conditions.Helpers;
using ThingsToDoInEorzea.Models;
using ThingsToDoInEorzea.Services;

namespace ThingsToDoInEorzea.Conditions.Modifiers
{
    /// <summary>
    /// Represents a condition that checks whether the current duty meets specific settings requirements, such as unrestricted party, level sync, etc.
    /// </summary>
    public class DutySettingsCondition : ModifierConditionBase
    {
        public bool RequireUnrestrictedParty { get; set; }
        public bool RequireLevelSync { get; set; }
        public bool RequireMinItemLevel { get; set; }
        public bool RequireSilenceEcho { get; set; }

        // Snapshot of what failed the condition, for logging purposes
        private bool failedUnrestrictedParty;
        private bool failedLevelSync;
        private bool failedMinItemLevel;
        private bool failedSilenceEcho;

        public override ClauseSectionKind Section => ClauseSectionKind.DutySettings;
        public override bool CheckOnStart => true;

        protected override void OnDutyStarted(GameEvent evt)
        {
            DutySettingsHelper.GetContentFinderSettings(
                out var isLevelSync,
                out var isMinItemLvl,
                out var isUnrestrictedParty,
                out var isSilenceEcho,
                out _);

            failedUnrestrictedParty = RequireUnrestrictedParty && !isUnrestrictedParty;
            // Level sync can only apply if the party is unrestricted
            failedLevelSync = RequireLevelSync && RequireUnrestrictedParty && !isLevelSync;
            failedMinItemLevel = RequireMinItemLevel && !isMinItemLvl;
            failedSilenceEcho = RequireSilenceEcho && !isSilenceEcho;

            LastKnownResult = !(failedUnrestrictedParty || failedLevelSync || failedMinItemLevel || failedSilenceEcho);
        }

        public override bool Matches(GameEvent evt)
        {
            switch (evt.Type)
            {
                case GameEventType.DutyStarted:
                    OnDutyStarted(evt);
                    return LastKnownResult ?? false;
                case GameEventType.DutyCompleted:
                    return LastKnownResult ?? false;
                default:
                    return base.Matches(evt);
            }
        }

        public override bool IsValid()
        {
            if (RequireLevelSync && !RequireUnrestrictedParty) return false;
            return RequireUnrestrictedParty || RequireMinItemLevel || RequireSilenceEcho;
        }

        public override IReadOnlyList<ClauseSegment> DescribeSegments(
            GameDataServices gameData, Configuration configuration)
        {
            var segments = new List<ClauseSegment>();
            if (RequireUnrestrictedParty)
            {
                segments.Add(ClauseSegment.ValueOnly(EvalSource.FromAddon(10936)));
                if (RequireLevelSync)
                    segments.Add(ClauseSegment.ValueOnly(EvalSource.FromAddon(12696)));
            }

            if (RequireMinItemLevel)
            {
                segments.Add(ClauseSegment.ValueOnly(EvalSource.FromAddon(10937)));
            }

            if (RequireSilenceEcho)
            {
                segments.Add(ClauseSegment.ValueOnly(EvalSource.FromAddon(12691)));
            }

            return segments;
        }

        public override string DescribeFailure(GameEvent evt, GameDataServices gameData)
        {
            var reasons = new List<string>();

            if (failedUnrestrictedParty) reasons.Add("    Party was not unrestricted.");
            if (failedLevelSync) reasons.Add("    Level Sync was not active.");
            if (failedMinItemLevel) reasons.Add("    Minimum IL (Item Level) was not enforced.");
            if (failedSilenceEcho) reasons.Add("    Echo was not silenced.");
            return reasons.Count > 0
                       ? $"Duty settings did not meet requirements: \n{string.Join("\n", reasons)}"
                       : "Duty settings did not meet requirements";
        }
    }
}
