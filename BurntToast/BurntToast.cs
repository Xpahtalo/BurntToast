using Dalamud.IoC;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using XivCommon;

namespace BurntToast;

public class BurntToast : IDalamudPlugin {
    public BurntToast() {
        Config = Interface.GetPluginConfig() as Configuration ?? new Configuration();
        Config.Initialise(this);

        Ui       = new PluginUi(this);
        Commands = new Commands(this);
        Common   = new XivCommonBase(Interface, Hooks.BattleTalk);
        Filter   = new Filter(this);
    }

    internal static string Name => "Burnt Toast";

    [PluginService] internal DalamudPluginInterface Interface { get; }

    [PluginService] internal IChatGui ChatGui { get; }

    [PluginService] internal ICommandManager CommandManager { get; }

    [PluginService] internal IToastGui ToastGui { get; }

    internal Configuration Config   { get; }
    internal PluginUi      Ui       { get; }
    internal XivCommonBase Common   { get; }
    private  Commands      Commands { get; }
    private  Filter        Filter   { get; }

    public void Dispose() {
        Filter.Dispose();
        Common.Dispose();
        Commands.Dispose();
        Ui.Dispose();
    }
}