using System.Threading;
using System.Threading.Tasks;
using Dalamud.Game.Command;
using Dalamud.Interface.Windowing;
using Dalamud.IoC;
using Dalamud.Plugin;
using ThingsToDoInEorzea.Services;
using ThingsToDoInEorzea.Windows;

namespace ThingsToDoInEorzea;

public sealed class Plugin : IAsyncDalamudPlugin
{
    private const string CommandName = "/ttd";
    private const string WindowCommandName = "/ttd window";

    [PluginService]
    internal static IDalamudPluginInterface PluginInterface { get; private set; } = null!;

    public Configuration Configuration { get; private set; } = null!;
    public GameDataServices GameDataServices { get; private set; } = null!;
    public ObjectiveTrackerService Tracker { get; private set; } = null!;
    public HookService HookService { get; private set; } = null!;
    public ObjectiveDispatchService DispatchService { get; private set; } = null!;

    public readonly WindowSystem WindowSystem = new("Things To Do In Eorzea");
    private ConfigWindow ConfigWindow { get; set; } = null!;
    private MainWindow MainWindow { get; set; } = null!;
    public ObjectiveWindow ObjectiveWindow { get; private set; } = null!;

    public Task LoadAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        // Initialize Dalamud services
        PluginInterface.Create<DalamudServices>();

        Configuration = PluginInterface.GetPluginConfig() as Configuration ?? new Configuration();

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

        PluginInterface.UiBuilder.Draw += WindowSystem.Draw;
        PluginInterface.UiBuilder.OpenConfigUi += ToggleConfigUi;
        PluginInterface.UiBuilder.OpenMainUi += ToggleMainUi;

        return Task.CompletedTask;
    }

    public ValueTask DisposeAsync()
    {
        DispatchService.Dispose();
        HookService.Dispose();
        Tracker.Dispose();
        GameDataServices.Dispose();

        PluginInterface.UiBuilder.Draw -= WindowSystem.Draw;
        PluginInterface.UiBuilder.OpenConfigUi -= ToggleConfigUi;
        PluginInterface.UiBuilder.OpenMainUi -= ToggleMainUi;

        WindowSystem.RemoveAllWindows();

        ConfigWindow.Dispose();
        MainWindow.Dispose();
        ObjectiveWindow.Dispose();

        DalamudServices.CommandManager.RemoveHandler(CommandName);

        return ValueTask.CompletedTask;
    }

    private void OnCommand(string command, string args) => MainWindow.Toggle();
    private void OnWindowCommand(string command, string args) => ObjectiveWindow.Toggle();

    public void ToggleConfigUi() => ConfigWindow.Toggle();
    public void ToggleMainUi() => MainWindow.Toggle();
}
