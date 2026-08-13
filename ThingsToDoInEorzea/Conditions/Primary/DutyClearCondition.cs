using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using ThingsToDoInEorzea.Conditions.Helpers;
using ThingsToDoInEorzea.Conditions.Modifiers;
using ThingsToDoInEorzea.Models;
using ThingsToDoInEorzea.Services;

namespace ThingsToDoInEorzea.Conditions.Primary
{
    /// <summary>
    /// Represents a primary condition that checks whether a specific duty has been cleared a certain number of times.
    /// </summary>
    public sealed class DutyClearCondition : PrimaryConditionBase
    {
        private const uint DutyClearOnceMacro = 15922;
        private const uint DutyClearMultipleMacro = 15961;

        public uint ContentFinderConditionId { get; set; }

        [JsonIgnore]
        public override IReadOnlySet<Type> AllowedModifierTypes => new HashSet<Type>
        {
            typeof(JobCondition),
            typeof(TimerCondition),
            typeof(WipeCondition),
            typeof(DeathCondition),
            typeof(DutySettingsCondition),
        };

        [JsonIgnore]
        public override int Current => CompletedDuties;
        public int CompletedDuties { get; set; }

        public override void OnGameEvent(GameEvent evt)
        {
            if (evt.Type != GameEventType.DutyCompleted) return;
            if (evt.ContentFinderConditionId != ContentFinderConditionId) return;

            CompletedDuties++;
            NotifyProgressChanged();
        }
        public override void ResetObjectiveProgress()
        {
            CompletedDuties = 0;
            NotifyProgressChanged();
        }
        public override bool IsValid() => ContentFinderConditionId > 0 && Target > 0;

        public override EvalSource DescribeActionSource(GameDataServices gameData)
        {
            return Target <= 1
                ? EvalSource.FromAddon(DutyClearOnceMacro, [0, 0, 0, 0, ContentFinderConditionId])
                : EvalSource.FromAddon(DutyClearMultipleMacro, [0, 0, 0, 0, ContentFinderConditionId, 0, 0, Target]);
        }
    }
}
