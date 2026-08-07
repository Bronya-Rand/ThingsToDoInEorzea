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
        int Target { get; set; }
        [JsonIgnore] bool ShowsProgress { get; }
        [JsonIgnore] float Progress => (Target <= 0 ? 1f : Math.Clamp((float)Current / Target, 0f, 1f));
        [JsonIgnore] IReadOnlySet<Type> AllowedModifierTypes { get; }
        string DescribeAction(GameDataServices gameData);
        EvalSource DescribeActionSource(GameDataServices gameData);
        bool IsValid();
    }
}
