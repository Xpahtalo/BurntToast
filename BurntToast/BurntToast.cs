using Dalamud.Game.Command;
using Dalamud.Game.Gui;
using Dalamud.Game.Gui.Toast;
using Dalamud.IoC;
using Dalamud.Plugin;
using XivCommon;

namespace BurntToast {
    public class BurntToast : IDalamudPlugin {
        public string Name => "Burnt Toast";

        [PluginService]
        internal DalamudPluginInterface Interface { get; private init; } = null!;

        [PluginService]
        internal ChatGui ChatGui { get; private init; } = null!;

        [PluginService]
        internal CommandManager CommandManager { get; private init; } = null!;

        [PluginService]
        internal ToastGui ToastGui { get; private init; } = null!;

        internal Configuration Config { get; }
        internal PluginUi Ui { get; }
        internal XivCommonBase Common { get; }
        private Commands Commands { get; }
        private Filter Filter { get; }

        public BurntToast() {
            this.Config = this.Interface.GetPluginConfig() as Configuration ?? new Configuration();
            this.Config.Initialise(this);

            this.Ui = new PluginUi(this);
            this.Commands = new Commands(this);
            this.Common = new XivCommonBase(Hooks.BattleTalk);
            this.Filter = new Filter(this);
        }

        public void Dispose() {
            this.Filter.Dispose();
            this.Common.Dispose();
            this.Commands.Dispose();
            this.Ui.Dispose();
        }
    }
}
