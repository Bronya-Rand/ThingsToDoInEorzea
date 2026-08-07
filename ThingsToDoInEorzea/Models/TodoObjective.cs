using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using Dalamud.Game.ClientState.Conditions;
using Dalamud.Game.DutyState;
using Dalamud.Game.Inventory;
using Dalamud.Game.Inventory.InventoryEventArgTypes;
using Dalamud.Utility;
using ThingsToDoInEorzea.Conditions.Helpers;
using ThingsToDoInEorzea.Conditions.Primary;
using ThingsToDoInEorzea.Services;

namespace ThingsToDoInEorzea.Models
{
    /// <summary>
    /// Represents a objective that a player can track, consisting of a primary condition and optional modifiers.
    /// </summary>
    [JsonPolymorphic(TypeDiscriminatorPropertyName = "$type")]
    [JsonDerivedType(typeof(TodoObjective), "todoObjective")]
    public class TodoObjective : IDisposable
    {
        private GameDataServices gameData = null!;
        private Configuration configuration = null!;

        public Guid Id { get; set; } = Guid.NewGuid();
        public ulong ContentId { get; set; } = 0; // The content ID of the player
        public string Title { get; set; } = string.Empty; // Title of the objective, e.g., "Heavyweight Blader"
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow; // Created timestamp
        public IPrimaryCondition Primary { get; set; } = null!; // The main objective to track
        public List<IModifierCondition> Modifiers { get; set; } = []; // List of modifiers that must be met
        [JsonIgnore] public bool IsComplete => Primary.IsComplete;
        [JsonIgnore] public bool IsFailed => Modifiers.Any(m => m.IsFailed);

        private bool isEnabled;
        public event Action<TodoObjective>? OnProgressChanged;

        public void Enable(GameDataServices gameData, Configuration configuration)
        {
            if (isEnabled) return;
            isEnabled = true;

            this.gameData = gameData;
            this.configuration = configuration;

            Primary.Enable();
            foreach (var modifier in Modifiers) modifier.Enable();

            Primary.OnProgressChanged += OnPrimaryChanged;

            DalamudServices.DutyState.DutyCompleted += OnDutyCompleted;
            DalamudServices.DutyState.DutyWiped += OnDutyWiped;
            DalamudServices.DutyState.DutyRecommenced += OnDutyRecommenced;
            DalamudServices.DutyState.DutyStarted += OnDutyStarted;
            DalamudServices.Condition.ConditionChange += OnConditionChanged;
            DalamudServices.GameInventory.ItemChanged += OnItemChanged;
        }

        public void Disable()
        {
            if (!isEnabled) return;
            isEnabled = false;

            Primary.Disable();
            foreach (var modifier in Modifiers) modifier.Disable();

            Primary.OnProgressChanged -= OnPrimaryChanged;

            DalamudServices.DutyState.DutyCompleted -= OnDutyCompleted;
            DalamudServices.DutyState.DutyStarted -= OnDutyStarted;
            DalamudServices.DutyState.DutyWiped -= OnDutyWiped;
            DalamudServices.DutyState.DutyRecommenced -= OnDutyRecommenced;
            DalamudServices.GameInventory.ItemChanged -= OnItemChanged;
            DalamudServices.Condition.ConditionChange -= OnConditionChanged;
        }

        #region Dalamud Event Handlers
        private void OnDutyStarted(IDutyStateEventArgs args) => HandleDutyEvent(GameEventType.DutyStarted, args);
        private void OnDutyCompleted(IDutyStateEventArgs args) => HandleDutyEvent(GameEventType.DutyCompleted, args);
        private void OnDutyWiped(IDutyStateEventArgs args) => HandleDutyEvent(GameEventType.DutyWiped, args);
        private void OnDutyRecommenced(IDutyStateEventArgs args) => HandleDutyEvent(GameEventType.DutyRecommenced, args);
        private void HandleDutyEvent(GameEventType eventType, IDutyStateEventArgs args)
        {
            var evt = new GameEvent
            {
                Type = eventType,
                TerritoryTypeId = args.TerritoryType.RowId,
                ClassJobId = DalamudServices.ObjectTable.LocalPlayer?.ClassJob.RowId ?? 0,
                ContentFinderConditionId = args.ContentFinderCondition.RowId,
                Timestamp = DateTime.UtcNow
            };

            if (Primary is DutyClearCondition dutyCond && dutyCond.ContentFinderConditionId != args.ContentFinderCondition.RowId)
                return;

            var modifierResults = Modifiers
                .Select(mod => (Modifier: mod, Matched: mod.Matches(evt, gameData, configuration)))
                .ToList();

            if (eventType == GameEventType.DutyStarted)
            {
                var warnings = modifierResults
                    .Where(r => r.Modifier.CheckOnStart && !r.Matched)
                    .Select(r => $"- {r.Modifier.DescribeFailure(evt, gameData)}")
                    .ToList();

                if (warnings.Count > 0)
                    gameData.NotificationService.SendInvalidMessage(Title, string.Join("\n", warnings));
            }

            if (modifierResults.All(r => r.Matched))
            {
                Primary.OnGameEvent(evt);
            }
            else if (eventType == GameEventType.DutyCompleted)
            {
                var failures = modifierResults
                    .Where(r => !r.Matched)
                    .Select(r => $"- {r.Modifier.DescribeFailure(evt, gameData)}")
                    .ToList();
                gameData.NotificationService.SendFailedMessage(Title, string.Join("\n", failures));
            }
        }
        private void OnItemChanged(GameInventoryEvent type, InventoryEventArgs data)
        {
            var evt = new GameEvent
            {
                Type = GameEventType.InventoryChanged,
                ItemId = data.Item.ItemId,
                IsHQ = data.Item.IsHq,
                Quantity = data.Item.Quantity,
                Timestamp = DateTime.UtcNow
            };
            if (Modifiers.All(mod => mod.Matches(evt, gameData, configuration)))
                Primary.OnGameEvent(evt);
        }
        private void OnConditionChanged(ConditionFlag flag, bool value)
        {
            GameEvent? evt = null;
            if (flag == ConditionFlag.Unconscious && value)
            {
                evt = new GameEvent
                {
                    Type = GameEventType.PlayerDied,
                    Timestamp = DateTime.UtcNow
                };
            }
            else if (flag == ConditionFlag.BoundByDuty && !value)
            {
                evt = new GameEvent
                {
                    Type = GameEventType.DutyAbandon,
                    Timestamp = DateTime.UtcNow
                };
            }
            if (evt != null && Modifiers.All(mod => mod.Matches(evt, gameData, configuration)))
                Primary.OnGameEvent(evt);
        }

        #endregion

        /// <summary>
        /// Handles non-Dalamud events that may affect the objective.
        /// </summary>
        /// <param name="evt"></param>
        internal void ReceiveExternalEvent(GameEvent evt)
        {
            if (!isEnabled) return;

            var modifierResults = Modifiers
                .Select(mod => (Modifier: mod, Matched: mod.Matches(evt, gameData, configuration)))
                .ToList();

            if (modifierResults.All(r => r.Matched))
                Primary.OnGameEvent(evt);
        }

        private void OnPrimaryChanged(ITrackCondition _) => OnProgressChanged?.Invoke(this);

        public string DescribeObjective(GameDataServices gameData, Configuration configuration)
        {
            var language = configuration?.LanguageOverride ?? DalamudServices.ClientState.ClientLanguage;
            var evaluator = DalamudServices.SeStringEvaluator;
            var loc = gameData.Localization;

            // If the primary condition does not show progress, return its description directly.
            if (!Primary.ShowsProgress)
                return Primary.DescribeAction(gameData);

            var actionSource = Primary.DescribeActionSource(gameData);
            var action = ClauseEvaluator.Resolve(actionSource, evaluator, loc, language);
            if (action.IsNullOrEmpty())
                action = Primary.DescribeAction(gameData);

            var progress = $"{Primary.Current}/{Primary.Target}";

            var resolvedSegments = Modifiers
                .SelectMany(mod => mod.DescribeSegments(gameData, configuration!))
                .Select(seg => ClauseEvaluator.ResolveSegment(seg, evaluator, loc, language))
                .Where(s => !string.IsNullOrEmpty(s.Value))
                .ToList();

            if (resolvedSegments.Count == 0)
                return $"{action} {progress}";

            var sectionList = ClauseFormatting.SectionOrder.ToList();
            var sections = Modifiers
                .GroupBy(m => m.Section)
                .OrderBy(g => sectionList.IndexOf(g.Key))
                .Select(g => (
                    Section: g.Key,
                    FormattedText: ClauseFormatting.FormatSection(
                        g.SelectMany(m => m.DescribeSegments(gameData, configuration!))
                         .Select(seg => ClauseEvaluator.ResolveSegment(seg, evaluator, loc, language)),
                        language
                    )))
                .Where(s => !string.IsNullOrEmpty(s.FormattedText))
                .ToList();

            var sb = new StringBuilder();
            sb.Append(action).Append(' ').Append(progress);
            foreach (var item in sections)
            {
                sb.AppendLine();
                var formatted = item.FormattedText;
                if (formatted.Length > 0)
                    formatted = $"{char.ToUpper(formatted[0])}{formatted[1..]}";
                sb.Append($"- ").Append(formatted);
            }
            return sb.ToString();
        }
        public void Dispose()
        {
            Disable();
            Primary.Dispose();
            foreach (var mod in Modifiers) mod.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}
