using Dalamud.IoC;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;

namespace ThingsToDoInEorzea
{
    internal class DalamudServices
    {
        [PluginService] internal static IDalamudPluginInterface PluginInterface { get; private set; } = null!;
        [PluginService] internal static ICommandManager CommandManager { get; private set; } = null!;
        [PluginService] internal static ISeStringEvaluator SeStringEvaluator { get; private set; } = null!;
        [PluginService] internal static IClientState ClientState { get; private set; } = null!;
        [PluginService] internal static IPlayerState PlayerState { get; private set; } = null!;
        [PluginService] internal static IDataManager DataManager { get; private set; } = null!;
        [PluginService] internal static IDutyState DutyState { get; private set; } = null!;
        [PluginService] internal static IObjectTable ObjectTable { get; private set; } = null!;
        [PluginService] internal static IGameInventory GameInventory { get; private set; } = null!;
        [PluginService] internal static IFramework Framework { get; private set; } = null!;
        [PluginService] internal static ICondition Condition { get; private set; } = null!;
        [PluginService] internal static IPluginLog Log { get; private set; } = null!;
        [PluginService] internal static IFateTable FateTable { get; private set; } = null!;
        [PluginService] internal static IChatGui ChatGui { get; private set; } = null!;
        [PluginService] public static IGameInteropProvider Hook { get; private set; } = null!;
        [PluginService] internal static IReliableFileStorage ReliableFileStorage { get; private set; } = null!;
    }
}
