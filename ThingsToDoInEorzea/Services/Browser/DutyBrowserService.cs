using Dalamud.Game;
using Dalamud.Plugin.Services;
using Dalamud.Utility;
using Lumina.Excel.Sheets;

namespace ThingsToDoInEorzea.Services.Browser
{
    public sealed class Duty
    {
        public uint RowId { get; set; }
        public string ShortCode { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public uint AcceptClassJobCategory { get; set; }
    }
    public sealed class DutyBrowserService(IDataManager dataManager, ISeStringEvaluator seStringEvaluator, Configuration configuration, ClientLanguage? overrideLanguage = null) : LuminaBrowserService<Duty, ContentFinderCondition>(dataManager, seStringEvaluator, configuration, overrideLanguage)
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
                entity = default!;
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
        public string GetDutyName(uint dutyId) => TryGet(dutyId, out var duty) ? duty!.Name : $"Unknown Duty ({dutyId})";
        public uint GetAcceptClassJobCategoryId(uint dutyId)
            => TryGet(dutyId, out var duty) ? duty!.AcceptClassJobCategory : 0;
        public bool IsTrialRaidUltimate(uint contentFinderConditionId)
        {
            if (contentFinderConditionId == 0) return false;
            var duty = dataManager.GetExcelSheet<ContentFinderCondition>()?.GetRow(contentFinderConditionId);
            if (duty == null) return false;
            var type = duty.Value.ContentType.RowId;
            return type == TrialContentTypeId || type == RaidContentTypeId || type == UltimateContentTypeId || type == ChaoticAllianceRaidContentTypeId;
        }
    }
}
