using System.Collections.Generic;
using System.Text.RegularExpressions;
using Dalamud.Configuration;

namespace BurntToast {
    public class Configuration : IPluginConfiguration {
        private BurntToast Plugin { get; set; } = null!;

        public int Version { get; set; } = 1;

        public List<Regex> Patterns { get; set; } = new List<Regex>();

        internal void Initialise(BurntToast plugin) {
            this.Plugin = plugin;
        }

        internal void Save() {
            this.Plugin.Interface.SavePluginConfig(this);
        }
    }
}
