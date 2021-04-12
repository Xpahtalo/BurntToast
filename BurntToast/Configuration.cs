using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Dalamud.Configuration;

namespace BurntToast {
    [Serializable]
    public class Configuration : IPluginConfiguration {
        private BurntToast Plugin { get; set; } = null!;

        public int Version { get; set; } = 1;

        public List<Regex> Patterns { get; set; } = new();

        public List<BattleTalkPattern> BattleTalkPatterns { get; set; } = new();

        internal void Initialise(BurntToast plugin) {
            this.Plugin = plugin;
        }

        internal void Save() {
            this.Plugin.Interface.SavePluginConfig(this);
        }
    }

    [Serializable]
    public class BattleTalkPattern {
        public Regex Pattern { get; set; }
        public bool ShowMessage { get; set; }

        public BattleTalkPattern(Regex pattern, bool showMessage) {
            this.Pattern = pattern;
            this.ShowMessage = showMessage;
        }
    }
}
