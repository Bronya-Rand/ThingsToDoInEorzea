using System;
using Dalamud.Plugin.Services;
using ThingsToDoInEorzea.Services.Browser;

namespace ThingsToDoInEorzea.Services
{
    /// <summary>
    /// Provides access to various game data services
    /// </summary>
    public sealed class GameDataServices : IDisposable
    {
        public ItemBrowserService ItemBrowserService { get; }
        public DutyBrowserService DutyBrowserService { get; }
        public JobBrowserService JobBrowserService { get; }
        public TerritoryBrowserService TerritoryBrowserService { get; }
        public FateBrowserService FateBrowserService { get; }
        public LocalizationService Localization { get; } = new();
        public NotificationService NotificationService { get; }

        public GameDataServices(IDataManager dataManager, ISeStringEvaluator seStringEvaluator, IGameInventory gameInventory, IPlayerState playerState, Configuration configuration)
        {
            ItemBrowserService = new(dataManager, seStringEvaluator, configuration);
            DutyBrowserService = new(dataManager, seStringEvaluator, configuration);
            JobBrowserService = new(dataManager, seStringEvaluator, gameInventory, playerState, configuration);
            TerritoryBrowserService = new(dataManager, seStringEvaluator, configuration);
            FateBrowserService = new(dataManager, seStringEvaluator, configuration);
            NotificationService = new(Localization);
        }

        public void Dispose()
        {
            ItemBrowserService.Dispose();
            DutyBrowserService.Dispose();
            JobBrowserService.Dispose();
            TerritoryBrowserService.Dispose();
            FateBrowserService.Dispose();
        }
    }
}
