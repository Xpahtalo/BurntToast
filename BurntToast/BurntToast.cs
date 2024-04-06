using System;
using System.Collections.Generic;
using Dalamud.Interface.Windowing;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using XivCommon;

namespace BurntToast;

public class BurntToast : IDalamudPlugin {
    private const int HistoryCapacity = 1000;

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

        Commands          = new Commands(this);
        Common            = new XivCommonBase(Interface, Hooks.BattleTalk);
        Filter            = new Filter(this);
        ToastHistory      = new Queue<ToastHistoryEntry>(HistoryCapacity);
        BattleTalkHistory = new Queue<BattleTalkHistoryEntry>(HistoryCapacity);
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

    internal Queue<BattleTalkHistoryEntry> BattleTalkHistory { get; }

    internal Queue<ToastHistoryEntry> ToastHistory { get; }

    private WindowSystem WindowSystem { get; } = new(Name);
    private Commands     Commands     { get; }
    private Filter       Filter       { get; }

    public void Dispose() {
        WindowSystem.RemoveAllWindows();

        Filter.Dispose();
        Common.Dispose();
        Commands.Dispose();
    }

    internal void AddToastHistory(string message, HandledType handledType, string regex = "") {
        if (ToastHistory.Count >= HistoryCapacity) {
            ToastHistory.Dequeue();
        }

        ToastHistory.Enqueue(new ToastHistoryEntry(message, DateTime.UtcNow, handledType, regex));
    }

    internal void AddBattleTalkHistory(string sender, string message, HandledType handledType, string regex = "") {
        if (BattleTalkHistory.Count >= HistoryCapacity) {
            BattleTalkHistory.Dequeue();
        }

        BattleTalkHistory.Enqueue(new BattleTalkHistoryEntry(sender, message, DateTime.UtcNow, handledType, regex));
    }
}

internal record HistoryEntry(
    string      Message,
    DateTime    Timestamp,
    HandledType HandledType,
    string      Regex);

internal record BattleTalkHistoryEntry : HistoryEntry {
    internal string Sender;

    internal BattleTalkHistoryEntry(string Sender, string Message, DateTime Timestamp, HandledType HandledType,
                                    string Regex)
        : base(Message, Timestamp, HandledType, Regex) {
        this.Sender = Sender;
    }
}

internal record ToastHistoryEntry : HistoryEntry {
    internal ToastHistoryEntry(string Message, DateTime Timestamp, HandledType HandledType, string Regex)
        : base(Message, Timestamp, HandledType, Regex) { }
}

internal enum HandledType {
    Passed,
    HandledExternally,
    Blocked,
}