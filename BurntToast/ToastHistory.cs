using System;
using System.Collections.Generic;

namespace BurntToast;

public class History {
    private const int HistoryCapacity = 1000;

    internal Queue<BattleTalkHistoryEntry> BattleTalkHistory { get; } = new(HistoryCapacity);
    internal Queue<ToastHistoryEntry>      ToastHistory      { get; } = new(HistoryCapacity);

    internal void AddToastHistory(ToastHistoryEntry entry) {
        if (ToastHistory.Count >= HistoryCapacity) { ToastHistory.Dequeue(); }
        ToastHistory.Enqueue(entry);
    }

    internal void AddBattleTalkHistory(BattleTalkHistoryEntry historyEntry) {
        if (BattleTalkHistory.Count >= HistoryCapacity) { BattleTalkHistory.Dequeue(); }
        BattleTalkHistory.Enqueue(historyEntry);
    }
}

internal record BattleTalkHistoryEntry(
    string      Sender,
    string      Message,
    DateTime    Timestamp,
    HandledType HandledType,
    string      Regex);

internal record ToastHistoryEntry(string Message, DateTime Timestamp, HandledType HandledType, string Regex);

public enum HandledType {
    Passed, HandledExternally, Blocked,
}