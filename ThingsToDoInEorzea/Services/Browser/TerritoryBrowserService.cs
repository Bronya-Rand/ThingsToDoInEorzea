using Dalamud.Game;
using Dalamud.Plugin.Services;
using Dalamud.Utility;
using Lumina.Excel.Sheets;

namespace ThingsToDoInEorzea.Services.Browser
{
    public class Territory
    {
        public uint RowId { get; init; }
        public string Name { get; init; } = string.Empty;
        public string Region { get; set; } = string.Empty;
        public uint PlaceNameRowId { get; init; }
    }

    public sealed class TerritoryBrowserService(
        IDataManager dataManager,
        ISeStringEvaluator seStringEvaluator,
        Configuration configuration,
        ClientLanguage? overrideLanguage = null)
        : LuminaBrowserService<Territory, TerritoryType>(dataManager, seStringEvaluator, configuration,
                                                         overrideLanguage)
    {
        protected override bool TryMapRow(TerritoryType row, out uint id, out Territory entity)
        {
            id = row.RowId;
            var name = EvaluateSeString(row.PlaceName.Value.Name);
            var region = EvaluateSeString(row.PlaceNameRegion.Value.Name);

            // Filter out invalid/empty rows, as well as instanced duties (dungeons, trials, raids, etc.)
            if (id == 0 || name.IsNullOrEmpty() || region.IsNullOrEmpty() || row.ContentFinderCondition.RowId != 0)
            {
                entity = null!;
                return false;
            }

            entity = new Territory
            {
                RowId = id,
                Name = name,
                Region = region,
                PlaceNameRowId = row.PlaceName.RowId,
            };
            return true;
        }

        public uint GetTerritoryPlaceNameRowId(uint territoryTypeRowId) =>
            TryGet(territoryTypeRowId, out var territory) ? territory!.PlaceNameRowId : 0;
    }
}
