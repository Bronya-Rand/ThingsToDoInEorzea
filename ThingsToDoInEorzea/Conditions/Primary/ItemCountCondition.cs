using System;
using System.Text.Json.Serialization;
using Dalamud.Game.ClientState.Conditions;
using FFXIVClientStructs.FFXIV.Client.Game;
using ThingsToDoInEorzea.Conditions.Helpers;
using ThingsToDoInEorzea.Models;
using ThingsToDoInEorzea.Services;

namespace ThingsToDoInEorzea.Conditions.Primary
{
    /// <summary>
    /// Represents a primary condition that checks whether the player has obtained a specific item(s).
    /// </summary>
    public sealed class ItemCountCondition : PrimaryConditionBase
    {
        private int currentCount;
        public uint ItemId { get; set; }
        public bool HighQualityOnly { get; set; }

        [JsonIgnore]
        public override int Current => Math.Max(0, FetchCurrentInventoryCount() - BaselineCount);

        public int BaselineCount { get; set; } // Handles daily/weekly collecting

        public ItemCountCondition() { } // For deserialization only

        public ItemCountCondition(uint itemId, int target, bool highQualityOnly)
        {
            ItemId = itemId;
            Target = target;
            HighQualityOnly = highQualityOnly;
        }

        public override void Enable() => RefreshCount();

        public override void ResetObjectiveProgress()
        {
            BaselineCount = FetchCurrentInventoryCount();
            RefreshCount();
        }

        public override void OnGameEvent(GameEvent evt)
        {
            if (evt.Type != GameEventType.InventoryChanged) return;
            if (evt.ItemId != 0 && evt.ItemId != ItemId) return;
            RefreshCount();
        }

        private void RefreshCount()
        {
            var newCount = FetchCurrentInventoryCount();
            if (newCount != currentCount)
            {
                currentCount = newCount;
                NotifyProgressChanged();
            }
        }

        public override bool IsValid()
        {
            if (ItemId <= 0 || Target <= 0) return false;

            // "Feature creep...": If the item is Gil (1) and player is on the 
            // Free Trial, target must be 300,000 or less.
            var onFreeTrial = DalamudServices.Condition[ConditionFlag.OnFreeTrial];
            return ItemId != 1 || !onFreeTrial || Target <= Constants.MaxGilOnFreeTrial;
        }

        public override EvalSource DescribeActionSource(GameDataServices gameData)
        {
            return EvalSource.FromLocKey("action.obtain_item", Target, ItemId);
        }

        /// <summary>
        /// Fetches the current count of the specified item in the player's inventory, 
        /// considering high-quality items if specified.
        /// </summary>
        /// <returns></returns>
        private unsafe int FetchCurrentInventoryCount()
        {
            var manager = InventoryManager.Instance();
            if (manager == null) return 0;

            var hqCount = manager->GetInventoryItemCount(ItemId, true);
            if (HighQualityOnly)
                return hqCount;

            return manager->GetInventoryItemCount(ItemId) + hqCount;
        }
    }
}
