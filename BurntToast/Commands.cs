using System;
using System.Collections.Generic;
using Dalamud.Game.Command;

namespace BurntToast;

public class Commands : IDisposable {
    private static readonly Dictionary<string, string> CommandList = new() {
        ["/burnttoast"] = "Opens the configuration for Burnt Toast",
        ["/bt"]         = "Alias for /burnttoast",
    };

    internal Commands(BurntToast plugin) {
        Plugin = plugin;

        foreach (var (name, desc) in CommandList) {
            Plugin.CommandManager.AddHandler(name, new CommandInfo(OnCommand) {
                HelpMessage = desc,
            });
        }
    }

    private BurntToast Plugin { get; }

    public void Dispose() {
        foreach (var name in CommandList.Keys) {
            Plugin.CommandManager.RemoveHandler(name);
        }
    }

    private void OnCommand(string command, string arguments) {
        Plugin.Ui.ToggleConfig();
    }
}