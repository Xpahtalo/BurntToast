using System;
using System.Collections.Generic;
using Dalamud.Game.Command;

namespace BurntToast {
    public class Commands : IDisposable {
        private static readonly Dictionary<string, string> CommandList = new Dictionary<string, string>() {
            ["/burnttoast"] = "Opens the configuration for Burnt Toast",
            ["/bt"] = "Alias for /burnttoast",
        };

        private BurntToast Plugin { get; }

        internal Commands(BurntToast plugin) {
            this.Plugin = plugin;

            foreach (var (name, desc) in CommandList) {
                this.Plugin.CommandManager.AddHandler(name, new CommandInfo(this.OnCommand) {
                    HelpMessage = desc,
                });
            }
        }

        public void Dispose() {
            foreach (var name in CommandList.Keys) {
                this.Plugin.CommandManager.RemoveHandler(name);
            }
        }

        private void OnCommand(string command, string arguments) {
            this.Plugin.Ui.ToggleConfig();
        }
    }
}
