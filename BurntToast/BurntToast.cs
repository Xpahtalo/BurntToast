using Dalamud.Interface.Windowing;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;

namespace BurntToast;

public sealed class BurntToast : IDalamudPlugin {
    public static string Name => "Burnt Toast";

    public IDalamudPluginInterface Interface       { get; }
    public IChatGui                ChatGui         { get; }
    public ICommandManager         CommandManager  { get; }
    public IGameInteropProvider    InteropProvider { get; }
    public IToastGui               ToastGui        { get; }
    public IPluginLog              Log             { get; }
    public Configuration           Config          { get; }
    public History                 History         { get; }

    public SettingsUi SettingsWindow { get; }
    public HistoryUi  HistoryWindow  { get; }

    public WindowSystem WindowSystem { get; } = new(Name);
    public Commands     Commands     { get; }
    public Filter       Filter       { get; }

    public BurntToast(
        IDalamudPluginInterface @interface,      IChatGui  chatGui,  ICommandManager commandManager,
        IGameInteropProvider    interopProvider, IToastGui toastGui, IPluginLog      log) {
        Interface       = @interface;
        ChatGui         = chatGui;
        CommandManager  = commandManager;
        InteropProvider = interopProvider;
        ToastGui        = toastGui;
        Log             = log;
        History         = new History();

        Config = Interface.GetPluginConfig() as Configuration ?? new Configuration();
        Config.Initialise(this);

        SettingsWindow = new SettingsUi(this);
        HistoryWindow  = new HistoryUi(this, History);
        WindowSystem.AddWindow(SettingsWindow);
        WindowSystem.AddWindow(HistoryWindow);

        Interface.UiBuilder.Draw         += WindowSystem.Draw;
        Interface.UiBuilder.OpenConfigUi += SettingsWindow.Toggle;

        Commands = new Commands(this);
        Filter   = new Filter(this, History, InteropProvider);
    }

    public void Dispose() {
        WindowSystem.RemoveAllWindows();
        Filter.Dispose();
        Commands.Dispose();
    }
}