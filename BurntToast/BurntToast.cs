using System;
using Dalamud.Plugin;

namespace BurntToast {
    public class BurntToast : IDalamudPlugin {
        public string Name => "Burnt Toast";

        internal DalamudPluginInterface Interface { get; private set; } = null!;
        internal Configuration Config { get; private set; } = null!;
        internal PluginUi Ui { get; private set; } = null!;
        private Commands Commands { get; set; } = null!;
        private Filter Filter { get; set; } = null!;

        public void Initialize(DalamudPluginInterface pluginInterface) {
            this.Interface = pluginInterface;

            this.Config = this.Interface.GetPluginConfig() as Configuration ?? new Configuration();
            this.Config.Initialise(this);

            this.Ui = new PluginUi(this);
            this.Commands = new Commands(this);
            this.Filter = new Filter(this);
        }

        public void Dispose() {
            this.Filter.Dispose();
            this.Commands.Dispose();
            this.Ui.Dispose();
        }
    }
}
