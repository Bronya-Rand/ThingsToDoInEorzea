using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.Json.Serialization;
using ThingsToDoInEorzea.Conditions.Modifiers;
using ThingsToDoInEorzea.Conditions.Primary;
using ThingsToDoInEorzea.Models;

namespace ThingsToDoInEorzea.Windows.Components
{
    internal static class ObjectiveConditionTypes
    {
        private static readonly Dictionary<Type, Func<IPrimaryCondition>> PrimaryFactories = new()
        {
            [typeof(ItemCountCondition)] = () => new ItemCountCondition { ItemId = 0, Target = 1 },
            [typeof(DutyClearCondition)] = () => new DutyClearCondition { ContentFinderConditionId = 0, Target = 1 },
            [typeof(CustomTextCondition)] = () => new CustomTextCondition(),
            [typeof(FateClearCondition)] = () => new FateClearCondition
                { MatchMode = FateMatchMode.SpecificFate, Target = 1 },
        };

        private static readonly Dictionary<Type, Func<IModifierCondition>> ModifierFactories = new()
        {
            [typeof(JobCondition)] = () => new JobCondition { ClassJobId = 0 },
            [typeof(TimerCondition)] = () => new TimerCondition { MaxDuration = TimeSpan.FromMinutes(5) },
            [typeof(WipeCondition)] = () => new WipeCondition { MaxAllowedWipes = 0 },
            [typeof(DeathCondition)] = () => new DeathCondition { MaxDeaths = 0 },
            [typeof(DutySettingsCondition)] = () => new DutySettingsCondition(),
        };

        public static readonly IReadOnlyList<Type> PrimaryTypes =
        [
            .. typeof(IPrimaryCondition)
               .GetCustomAttributes<JsonDerivedTypeAttribute>()
               .Select(a => a.DerivedType)
        ];

        public static IPrimaryCondition Create(Type type) =>
            PrimaryFactories.TryGetValue(type, out var factory)
                ? factory()
                : throw new NotImplementedException($"Primary condition type {type.Name} not implemented.");

        public static IModifierCondition CreateModifier(Type type) =>
            ModifierFactories.TryGetValue(type, out var factory)
                ? factory()
                : throw new NotImplementedException($"Modifier condition type {type.Name} not implemented.");

        public static readonly IReadOnlyList<Type> ModifierTypes =
        [
            .. typeof(IModifierCondition)
               .GetCustomAttributes<JsonDerivedTypeAttribute>()
               .Select(a => a.DerivedType)
        ];
    }
}
