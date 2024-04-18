using System;
using System.Collections.Generic;

namespace BurntToast;

public class History {
    private const int HistoryCapacity = 1000;

    internal Queue<BattleTalkHistoryEntry> BattleTalkHistory { get; } = new(HistoryCapacity);
    internal Queue<ToastHistoryEntry>      ToastHistory      { get; } = new(HistoryCapacity);

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
    internal readonly string Sender;

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