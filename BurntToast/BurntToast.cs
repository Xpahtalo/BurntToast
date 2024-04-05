using System;
using System.Collections.Generic;
using System.Linq;
using Dalamud.Interface.Windowing;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using XivCommon;

namespace BurntToast;

public class BurntToast : IDalamudPlugin {
    public BurntToast(DalamudPluginInterface @interface,     IChatGui  chatGui,
                      ICommandManager        commandManager, IToastGui toastGui,
                      IPluginLog             log) {
        Interface      = @interface;
        ChatGui        = chatGui;
        CommandManager = commandManager;
        ToastGui       = toastGui;
        Log            = log;

        Config = Interface.GetPluginConfig() as Configuration ?? new Configuration();
        Config.Initialise(this);

        SettingsWindow = new SettingsUi(this);
        HistoryWindow  = new HistoryUi(this, SettingsWindow);
        WindowSystem.AddWindow(SettingsWindow);
        WindowSystem.AddWindow(HistoryWindow);

        Interface.UiBuilder.Draw         += WindowSystem.Draw;
        Interface.UiBuilder.OpenConfigUi += SettingsWindow.Toggle;

        Commands = new Commands(this);
        Common   = new XivCommonBase(Interface, Hooks.BattleTalk);
        Filter   = new Filter(this);
        History  = new Queue<HistoryEntry>();
    }

    internal static string Name => "Burnt Toast";

    internal DalamudPluginInterface Interface      { get; }
    internal IChatGui               ChatGui        { get; }
    internal ICommandManager        CommandManager { get; }
    internal IToastGui              ToastGui       { get; }
    internal IPluginLog             Log            { get; }
    internal Configuration          Config         { get; }
    internal XivCommonBase          Common         { get; }

    internal SettingsUi SettingsWindow { get; }
    internal HistoryUi  HistoryWindow  { get; }

    internal IEnumerable<HistoryEntry> ToastHistory => History.Where(entry => entry.Type == HistoryType.Toast);

    internal IEnumerable<HistoryEntry> BattleTalkHistory =>
        History.Where(entry => entry.Type == HistoryType.BattleTalk);

    private WindowSystem        WindowSystem { get; } = new(Name);
    private Queue<HistoryEntry> History      { get; }
    private Commands            Commands     { get; }
    private Filter              Filter       { get; }

    public void Dispose() {
        WindowSystem.RemoveAllWindows();

        Filter.Dispose();
        Common.Dispose();
        Commands.Dispose();
    }

    internal void AddHistory(string text, HistoryType type, HandledType blocked, string regex = "") {
        if (History.Count >= 1000) {
            History.Dequeue();
        }

        History.Enqueue(new HistoryEntry(DateTime.UtcNow, text, type, blocked, regex));
    }
}

public record HistoryEntry(DateTime Timestamp, string Text, HistoryType Type, HandledType HandledType, string Regex);

public enum HistoryType {
    Toast,
    BattleTalk,
}

public enum HandledType {
    Passed,
    HandledExternally,
    Blocked,
}