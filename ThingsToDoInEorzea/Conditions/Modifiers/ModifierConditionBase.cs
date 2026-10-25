using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using ThingsToDoInEorzea.Conditions.Helpers;
using ThingsToDoInEorzea.Models;
using ThingsToDoInEorzea.Services;

namespace ThingsToDoInEorzea.Conditions.Modifiers
{
    /// <summary>
    /// The base class for all modifier conditions.
    /// </summary>
    public abstract class ModifierConditionBase : IModifierCondition
    {
        protected bool? LastKnownResult = null;
        public virtual void Enable() { }
        public virtual void Disable() { }
        public virtual bool IsValid() => true;

        /// <summary>
        /// Whether to check this condition at the start of an event (e.g., when a duty starts).
        /// </summary>
        [JsonIgnore]
        public virtual bool CheckOnStart => false;

        [JsonIgnore]
        public virtual float? Progress => null;

        [JsonIgnore]
        public virtual bool IsFailed => LastKnownResult.HasValue && !LastKnownResult.Value;

        [JsonIgnore]
        public virtual ClauseSectionKind Section => ClauseSectionKind.Requirements;

        public virtual bool Matches(GameEvent evt)
        {
            switch (evt.Type)
            {
                case GameEventType.DutyStarted:
                    OnDutyStarted(evt);
                    return true;
                case GameEventType.DutyAbandon:
                    return LastKnownResult ?? false;
                default:
                    return LastKnownResult ?? true;
            }
        }

        public virtual bool Matches(GameEvent evt, GameDataServices gameData, Configuration configuration) =>
            Matches(evt);

        protected virtual void OnDutyStarted(GameEvent evt) { }
        public virtual ClauseSegment? DescribeSegment(GameDataServices gameData, Configuration configuration) => null;

        public virtual IReadOnlyList<ClauseSegment> DescribeSegments(
            GameDataServices gameData, Configuration configuration)
        {
            var single = DescribeSegment(gameData, configuration);
            return single.HasValue ? [single.Value] : Array.Empty<ClauseSegment>();
        }

        public virtual string DescribeFailure(GameEvent evt, GameDataServices gameData) =>
            string.Empty;

        public virtual string DescribeFailure(GameEvent evt, GameDataServices gameData, Configuration configuration) =>
            DescribeFailure(evt, gameData);

        public virtual void Dispose() => GC.SuppressFinalize(this);
    }
}
