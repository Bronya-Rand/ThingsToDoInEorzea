using System.Threading;
using Dalamud.Game;
using Dalamud.Plugin.Services;
using Lumina.Excel;
using Lumina.Text.ReadOnly;

namespace ThingsToDoInEorzea.Services.Browser
{
    /// <summary>
    /// A base class for services that browse and map Lumina Excel sheets to entities of type <typeparamref name="TEntity"/>.
    /// </summary>
    /// <typeparam name="TEntity">The type of entity to map to.</typeparam>
    /// <typeparam name="TLuminaRow">The type of Lumina row to read from.</typeparam>
    /// <param name="dataManager">Dalamud's data manager.</param>
    /// <param name="seStringEvaluator">The in-game string evaluator.</param>
    /// <param name="configuration">Plugin configuration.</param>
    /// <param name="overrideLanguage">The language to override the default language to.</param>
    public abstract class LuminaBrowserService<TEntity, TLuminaRow>(IDataManager dataManager, ISeStringEvaluator seStringEvaluator, Configuration configuration, ClientLanguage? overrideLanguage = null) : BrowserServiceBase<uint, TEntity>(dataManager, configuration)
        where TLuminaRow : struct, IExcelRow<TLuminaRow>
    {
        protected readonly ISeStringEvaluator seStringEvaluator = seStringEvaluator;
        protected readonly ClientLanguage? OverrideLanguage = overrideLanguage; // For testing purposes

        protected string EvaluateSeString(ReadOnlySeString seString)
        {
            if (seString.IsEmpty) return string.Empty;
            return seStringEvaluator.Evaluate(seString).ToString() ?? string.Empty;
        }
        protected override void LoadData(CancellationToken token)
        {
            var excelSheet = OverrideLanguage.HasValue
                ? dataManager.GetExcelSheet<TLuminaRow>(OverrideLanguage.Value)
                : dataManager.GetExcelSheet<TLuminaRow>();

            if (excelSheet == null) return;

            foreach (var row in excelSheet)
            {
                if (token.IsCancellationRequested) return;

                if (TryMapRow(row, out var id, out var entity) && id != 0)
                {
                    Entries[id] = entity;
                }
            }
        }
        protected abstract bool TryMapRow(TLuminaRow row, out uint id, out TEntity entity);
    }
}
