using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using ThingsToDoInEorzea.Conditions.Helpers;
using ThingsToDoInEorzea.Conditions.Primary;
using ThingsToDoInEorzea.Services;

namespace ThingsToDoInEorzea.Models
{
    [JsonPolymorphic(TypeDiscriminatorPropertyName = "$type")]
    [JsonDerivedType(typeof(ItemCountCondition), "itemCount")]
    [JsonDerivedType(typeof(DutyClearCondition), "dutyClear")]
    [JsonDerivedType(typeof(CustomTextCondition), "custom")]
    [JsonDerivedType(typeof(FateClearCondition), "fateClear")]
    public interface IPrimaryCondition : ITrackCondition
    {
        [JsonIgnore] int Current { get; }
        int Target { get; }
        [JsonIgnore] bool ShowsProgress { get; }
        [JsonIgnore] IReadOnlySet<Type> AllowedModifierTypes { get; }
        string DescribeAction();
        EvalSource DescribeActionSource(GameDataServices gameData);
        void ResetObjectiveProgress();
        bool IsValid();
    }
}
