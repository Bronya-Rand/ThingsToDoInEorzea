using Dalamud.Game;
using Dalamud.Plugin.Services;
using Dalamud.Utility;
using Lumina.Excel.Sheets;

namespace ThingsToDoInEorzea.Services.Browser
{
    public sealed class FateInfo
    {
        public uint RowId { get; init; }
        public string Name { get; init; } = string.Empty;
    }

    public sealed class FateBrowserService(
        IDataManager dataManager,
        ISeStringEvaluator seStringEvaluator,
        Configuration configuration,
        ClientLanguage? overrideLanguage = null)
        : LuminaBrowserService<FateInfo, Fate>(dataManager, seStringEvaluator, configuration, overrideLanguage)
    {
        protected override bool TryMapRow(Fate row, out uint id, out FateInfo entity)
        {
            id = row.RowId;
            var name = EvaluateSeString(row.Name);

            if (id == 0 || name.IsNullOrEmpty())
            {
                entity = null!;
                return false;
            }

            entity = new FateInfo
            {
                RowId = id,
                Name = name
            };
            return true;
        }
    }
}
