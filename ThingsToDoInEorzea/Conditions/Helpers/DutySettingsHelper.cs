using Dalamud.Game.Gui.PartyFinder.Types;
using FFXIVClientStructs.FFXIV.Client.Game.UI;

namespace ThingsToDoInEorzea.Conditions.Helpers
{
    /// <summary>
    /// Retrieves and interprets duty settings from the game's content finder.
    /// </summary>
    public static unsafe class DutySettingsHelper
    {
        public static void GetContentFinderSettings(out bool isLevelSync, out bool isMinIL, out bool isUnrestrictedParty, out bool isSilenceEcho, out LootRuleFlags lootRuleFlags)
        {
            var contentFinder = ContentsFinder.Instance();
            if (contentFinder == null)
            {
                isLevelSync = false;
                isMinIL = false;
                isUnrestrictedParty = false;
                isSilenceEcho = false;
                lootRuleFlags = LootRuleFlags.Normal;
                return;
            }

            isLevelSync = contentFinder->IsLevelSync;
            isMinIL = contentFinder->IsMinimalIL;
            isUnrestrictedParty = contentFinder->IsUnrestrictedParty;
            isSilenceEcho = contentFinder->IsSilenceEcho;
            var flags = contentFinder->LootRules switch
            {
                ContentsFinder.LootRule.Normal => LootRuleFlags.Normal,
                ContentsFinder.LootRule.GreedOnly => LootRuleFlags.GreedOnly,
                ContentsFinder.LootRule.Lootmaster => LootRuleFlags.Lootmaster,
                _ => LootRuleFlags.Normal,
            };
            lootRuleFlags = flags;
        }
    }
}
