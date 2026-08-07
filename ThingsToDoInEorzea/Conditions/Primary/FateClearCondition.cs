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
    /// Represents a primary condition that checks whether a specific FATE has been cleared a certain number of times.
    /// </summary>
    public class FateClearCondition : PrimaryConditionBase
    {
        private const uint FateClearOnceMacro = 8340;

        private int currentCount;

        public FateMatchMode MatchMode { get; set; } = FateMatchMode.SpecificFate;

        // For SpecificFate
        public uint? FateRowId { get; set; }

        // For AnyInRegion
        public uint? TerritoryTypeRowId { get; set; }

        [JsonIgnore]
        public override int Current => currentCount;

        [JsonIgnore]
        public override IReadOnlySet<Type> AllowedModifierTypes { get; } =
            new HashSet<Type> { typeof(JobCondition) };

        // Needed for persistence, no live-game state avail
        public int PersistentCount
        {
            get => currentCount;
            set => currentCount = value;
        }

        public FateClearCondition() { } // For deserialization only
        public FateClearCondition(FateMatchMode matchMode, int target, uint? fateRowId = null, uint? territoryTypeRowId = null)
        {
            MatchMode = matchMode;
            Target = target;
            FateRowId = fateRowId;
            TerritoryTypeRowId = territoryTypeRowId;
        }

        public override void OnGameEvent(GameEvent evt)
        {
            if (evt.Type != GameEventType.FateCompleted) return;
            if (!MatchesEvent(evt)) return;

            currentCount++;
            NotifyProgressChanged();
        }
        public override EvalSource DescribeActionSource(GameDataServices gameData) => MatchMode switch
        {
            FateMatchMode.SpecificFate => EvalSource.FromAddon(FateClearOnceMacro, [FateRowId ?? 0]),
            FateMatchMode.AnyInRegion => EvalSource.FromLocKey("action.fate_region", [Target, // TODO: Change Macro back to TerritoryTypeRowId 
                gameData.TerritoryBrowserService.GetTerritoryPlaceNameRowId(TerritoryTypeRowId ?? 0)]),
            FateMatchMode.AnyFate => EvalSource.FromLocKey("action.fate_any", [Target]),
            _ => EvalSource.FromMacro(DescribeAction(gameData))
        };
        public override bool IsValid() => MatchMode switch
        {
            FateMatchMode.SpecificFate => FateRowId.HasValue && FateRowId.Value > 0 && Target > 0,
            FateMatchMode.AnyInRegion => TerritoryTypeRowId.HasValue && TerritoryTypeRowId.Value > 0 && Target > 0,
            FateMatchMode.AnyFate => Target > 0,
            _ => false
        };
        private bool MatchesEvent(GameEvent evt) => MatchMode switch
        {
            FateMatchMode.SpecificFate => evt.FateRowId == FateRowId,
            FateMatchMode.AnyInRegion => evt.TerritoryTypeId == TerritoryTypeRowId,
            FateMatchMode.AnyFate => true,
            _ => false
        };
    }
}
