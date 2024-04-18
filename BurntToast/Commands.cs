using System;
using System.Collections.Generic;
using Dalamud.Game.Command;

namespace BurntToast;

public class Commands : IDisposable {
    private List<Command> CommandList { get; }
    private BurntToast    Plugin      { get; }

    internal Commands(BurntToast plugin) {
        Plugin = plugin;

        CommandList = new List<Command> {
            new("/burnttoast", "Opens the configuration for Burnt Toast", ToggleSettings),
            new("/burnttoasthistory", "Opens the toast history window", ToggleHistory),
            new("/bt", "Alias for /burnttoast", ToggleSettings),
            new("/bth", "Alias for /burnttoasthistory", ToggleHistory),
        };
        foreach (var command in CommandList) {
            Plugin.Log.Debug("Adding command {0} with the description {1}", command.Name, command.Description);
            Plugin.CommandManager.AddHandler(command.Name, new CommandInfo(OnCommand) {
                HelpMessage = command.Description,
            });
        }
    }

    public void Dispose() {
        foreach (var command in CommandList) {
            Plugin.Log.Debug("Removing command {0}", command.Name);
            Plugin.CommandManager.RemoveHandler(command.Name);
        }
    }

    private void OnCommand(string command, string arguments) {
        CommandList.Find(c => string.Equals(command, c.Name, StringComparison.OrdinalIgnoreCase))?.Action();
    }

    private void ToggleHistory() {
        Plugin.HistoryWindow.Toggle();
    }

    private void ToggleSettings() {
        Plugin.SettingsWindow.Toggle();
    }

    private record Command(string Name, string Description, Action Action);
}