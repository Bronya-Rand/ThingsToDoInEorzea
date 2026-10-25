using Dalamud.Game;
using Dalamud.Plugin.Services;
using Dalamud.Utility;
using Lumina.Excel.Sheets;

namespace ThingsToDoInEorzea.Services.Browser
{
    public sealed class Duty
    {
        public uint RowId { get; init; }
        public string ShortCode { get; init; } = string.Empty;
        public string Name { get; init; } = string.Empty;
        public uint AcceptClassJobCategory { get; init; }
    }

    public sealed class DutyBrowserService(
        IDataManager dataManager,
        ISeStringEvaluator seStringEvaluator,
        Configuration configuration,
        ClientLanguage? overrideLanguage = null)
        : LuminaBrowserService<Duty, ContentFinderCondition>(dataManager, seStringEvaluator, configuration,
                                                             overrideLanguage)
    {
        // ContentType.RowId
        private const uint TrialContentTypeId = 4;
        private const uint RaidContentTypeId = 5;
        private const uint UltimateContentTypeId = 28;
        private const uint ChaoticAllianceRaidContentTypeId = 37;

        protected override bool TryMapRow(ContentFinderCondition row, out uint id, out Duty entity)
        {
            id = row.RowId;
            var name = EvaluateSeString(row.Name);
            var shortCode = row.ShortCode.ToString();

            if (id == 0 || name.IsNullOrEmpty() || shortCode.IsNullOrEmpty())
            {
                entity = null!;
                return false;
            }

            entity = new Duty
            {
                RowId = id,
                Name = name,
                ShortCode = shortCode,
                AcceptClassJobCategory = row.AcceptClassJobCategory.RowId,
            };
            return true;
        }

        public uint GetAcceptClassJobCategoryId(uint dutyId)
            => TryGet(dutyId, out var duty) ? duty!.AcceptClassJobCategory : 0;

        public bool IsTrialRaidUltimate(uint contentFinderConditionId)
        {
            if (contentFinderConditionId == 0) return false;
            var duty = DataManager.GetExcelSheet<ContentFinderCondition>().GetRow(contentFinderConditionId);
            var type = duty.ContentType.RowId;
            return type is TrialContentTypeId or RaidContentTypeId or UltimateContentTypeId
                       or ChaoticAllianceRaidContentTypeId;
        }
    }
}
