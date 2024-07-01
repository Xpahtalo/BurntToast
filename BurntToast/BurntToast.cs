using Dalamud.Interface.Windowing;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;

namespace BurntToast;

public class BurntToast : IDalamudPlugin {
    private static string Name => "Burnt Toast";

    internal IDalamudPluginInterface Interface       { get; }
    internal IChatGui                ChatGui         { get; }
    internal ICommandManager         CommandManager  { get; }
    internal IGameInteropProvider    InteropProvider { get; }
    internal IToastGui               ToastGui        { get; }
    internal IPluginLog              Log             { get; }
    internal Configuration           Config          { get; }
    private  History                 History         { get; }

    internal SettingsUi SettingsWindow { get; }
    internal HistoryUi  HistoryWindow  { get; }

    private WindowSystem WindowSystem { get; } = new(Name);
    private Commands     Commands     { get; }
    private Filter       Filter       { get; }

    public BurntToast(IDalamudPluginInterface @interface,     IChatGui  chatGui,
                      ICommandManager        commandManager, IGameInteropProvider interopProvider,IToastGui toastGui,
                      IPluginLog             log) {
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
        Filter   = new Filter(this,  History);
    }

    public void Dispose() {
        WindowSystem.RemoveAllWindows();

        Filter.Dispose();
        Commands.Dispose();
    }
}