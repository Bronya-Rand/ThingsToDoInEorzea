using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using ThingsToDoInEorzea.Conditions.Helpers;
using ThingsToDoInEorzea.Conditions.Modifiers;
using ThingsToDoInEorzea.Services;

namespace ThingsToDoInEorzea.Models
{
    [JsonPolymorphic(TypeDiscriminatorPropertyName = "$type")]
    [JsonDerivedType(typeof(JobCondition), "job")]
    [JsonDerivedType(typeof(DutySettingsCondition), "dutySettings")]
    [JsonDerivedType(typeof(TimerCondition), "timeLimit")]
    [JsonDerivedType(typeof(WipeCondition), "wipeLimit")]
    [JsonDerivedType(typeof(DeathCondition), "deathLimit")]
    public interface IModifierCondition : IDisposable
    {
        void Enable();
        void Disable();

        [JsonIgnore]
        bool CheckOnStart { get; }

        [JsonIgnore]
        ClauseSectionKind Section { get; }

        [JsonIgnore]
        float? Progress { get; }

        [JsonIgnore]
        bool IsFailed => false;

        bool Matches(GameEvent evt);
        bool Matches(GameEvent evt, GameDataServices gameData, Configuration configuration);
        ClauseSegment? DescribeSegment(GameDataServices gameData, Configuration configuration);
        IReadOnlyList<ClauseSegment> DescribeSegments(GameDataServices gameData, Configuration configuration);
        string DescribeFailure(GameEvent evt, GameDataServices gameData);
        string DescribeFailure(GameEvent evt, GameDataServices gameData, Configuration configuration);
        bool IsValid();
    }
}
