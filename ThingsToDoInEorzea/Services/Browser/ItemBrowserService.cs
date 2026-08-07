using Dalamud.Game;
using Dalamud.Plugin.Services;
using Dalamud.Utility;
using Lumina.Excel.Sheets;

namespace ThingsToDoInEorzea.Services.Browser
{
    public sealed class GameItem
    {
        public uint RowId { get; set; }
        public string SingularName { get; set; } = string.Empty;
        public string PluralName { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public bool CanBeHq { get; set; }
        public uint MaxStackSize { get; set; }
    }
    public sealed class ItemBrowserService(IDataManager dataManager, ISeStringEvaluator seStringEvaluator, Configuration configuration, ClientLanguage? overrideLanguage = null) : LuminaBrowserService<GameItem, Item>(dataManager, seStringEvaluator, configuration, overrideLanguage)
    {
        protected override bool TryMapRow(Item row, out uint id, out GameItem entity)
        {
            id = row.RowId;
            var name = EvaluateSeString(row.Name);
            var stackSize = row.StackSize;

            if (id == 0 || name.IsNullOrEmpty() || stackSize == 0)
            {
                entity = default!;
                return false;
            }

            entity = new GameItem
            {
                RowId = id,
                Name = name,
                SingularName = EvaluateSeString(row.Singular),
                PluralName = EvaluateSeString(row.Plural),
                CanBeHq = row.CanBeHq,
                MaxStackSize = stackSize
            };
            return true;
        }
        public string GetItemName(uint itemId) => TryGet(itemId, out var item) ? item!.Name : $"Unknown Item ({itemId})";
        public string GetItemSingularName(uint itemId) => TryGet(itemId, out var item) ? item!.SingularName : $"Unknown Item ({itemId})";
        public string GetItemPluralName(uint itemId) => TryGet(itemId, out var item) ? item!.PluralName : $"Unknown Item ({itemId})";
        public uint GetItemStackSize(uint itemId) => TryGet(itemId, out var item) ? item!.MaxStackSize : 0;
    }
}
