using Dalamud.Game.Command;
using Dalamud.Interface.Windowing;
using Dalamud.Plugin;
using ThingsToDoInEorzea.Services;
using ThingsToDoInEorzea.Windows;

namespace ThingsToDoInEorzea;

public sealed class Plugin : IDalamudPlugin
{
    private const string CommandName = "/ttd";
    private const string WindowCommandName = "/ttd window";

    public Configuration Configuration { get; init; }
    public GameDataServices GameDataServices { get; init; }
    public ObjectiveTrackerService Tracker { get; init; }
    public HookService HookService { get; init; }
    public ObjectiveDispatchService DispatchService { get; init; }

    public readonly WindowSystem WindowSystem = new("Things To Do In Eorzea");
    private ConfigWindow ConfigWindow { get; init; }
    private MainWindow MainWindow { get; init; }
    public ObjectiveWindow ObjectiveWindow { get; init; }

    public Plugin(IDalamudPluginInterface pluginInterface)
    {
        // Initialize Dalamud services
        pluginInterface.Create<DalamudServices>();

        Configuration = DalamudServices.PluginInterface.GetPluginConfig() as Configuration ?? new Configuration();

        // Initialize services
        GameDataServices = new GameDataServices(
            DalamudServices.DataManager,
            DalamudServices.SeStringEvaluator,
            DalamudServices.GameInventory,
            DalamudServices.PlayerState,
            Configuration
        );
        Tracker = new ObjectiveTrackerService(GameDataServices, Configuration, DalamudServices.ReliableFileStorage);
        HookService = new HookService();
        DispatchService = new ObjectiveDispatchService(HookService, Tracker);

        // Initialize windows
        ConfigWindow = new ConfigWindow(this);
        MainWindow = new MainWindow(this);
        ObjectiveWindow = new ObjectiveWindow(this);

        WindowSystem.AddWindow(ConfigWindow);
        WindowSystem.AddWindow(MainWindow);
        WindowSystem.AddWindow(ObjectiveWindow);

        // Register commands and UI events
        DalamudServices.CommandManager.AddHandler(CommandName, new CommandInfo(OnCommand)
        {
            HelpMessage = $"Opens the {Constants.PluginName} window."
        });

        DalamudServices.CommandManager.AddHandler(WindowCommandName, new CommandInfo(OnWindowCommand)
        {
            HelpMessage = "Opens the objective window (if closed accidentally)."
        });

        DalamudServices.PluginInterface.UiBuilder.Draw += WindowSystem.Draw;
        DalamudServices.PluginInterface.UiBuilder.OpenConfigUi += ToggleConfigUi;
        DalamudServices.PluginInterface.UiBuilder.OpenMainUi += ToggleMainUi;
    }

    public void Dispose()
    {
        DispatchService.Dispose();
        HookService.Dispose();
        Tracker.Dispose();
        GameDataServices.Dispose();

        DalamudServices.PluginInterface.UiBuilder.Draw -= WindowSystem.Draw;
        DalamudServices.PluginInterface.UiBuilder.OpenConfigUi -= ToggleConfigUi;
        DalamudServices.PluginInterface.UiBuilder.OpenMainUi -= ToggleMainUi;

        WindowSystem.RemoveAllWindows();

        ConfigWindow.Dispose();
        MainWindow.Dispose();
        ObjectiveWindow.Dispose();

        DalamudServices.CommandManager.RemoveHandler(CommandName);
    }

    private void OnCommand(string command, string args) => MainWindow.Toggle();
    private void OnWindowCommand(string command, string args) => ObjectiveWindow.Toggle();

    public void ToggleConfigUi() => ConfigWindow.Toggle();
    public void ToggleMainUi() => MainWindow.Toggle();
}
