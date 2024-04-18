using Dalamud.Interface.Windowing;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using XivCommon;

namespace BurntToast;

public class BurntToast : IDalamudPlugin {
    private static string Name => "Burnt Toast";

    internal DalamudPluginInterface Interface      { get; }
    internal IChatGui               ChatGui        { get; }
    internal ICommandManager        CommandManager { get; }
    internal IToastGui              ToastGui       { get; }
    internal IPluginLog             Log            { get; }
    internal Configuration          Config         { get; }
    internal XivCommonBase          Common         { get; }
    private  History                History        { get; }

    internal SettingsUi SettingsWindow { get; }
    internal HistoryUi  HistoryWindow  { get; }

    private WindowSystem WindowSystem { get; } = new(Name);
    private Commands     Commands     { get; }
    private Filter       Filter       { get; }

    public BurntToast(DalamudPluginInterface @interface,     IChatGui  chatGui,
                      ICommandManager        commandManager, IToastGui toastGui,
                      IPluginLog             log) {
        Interface      = @interface;
        ChatGui        = chatGui;
        CommandManager = commandManager;
        ToastGui       = toastGui;
        Log            = log;
        History        = new History();

        Config = Interface.GetPluginConfig() as Configuration ?? new Configuration();
        Config.Initialise(this);

        SettingsWindow = new SettingsUi(this);
        HistoryWindow  = new HistoryUi(this, History);
        WindowSystem.AddWindow(SettingsWindow);
        WindowSystem.AddWindow(HistoryWindow);

        Interface.UiBuilder.Draw         += WindowSystem.Draw;
        Interface.UiBuilder.OpenConfigUi += SettingsWindow.Toggle;

        Commands = new Commands(this);
        Common   = new XivCommonBase(Interface, Hooks.BattleTalk);
        Filter   = new Filter(this, History);
    }

    public void Dispose() {
        WindowSystem.RemoveAllWindows();

        Filter.Dispose();
        Common.Dispose();
        Commands.Dispose();
    }
}