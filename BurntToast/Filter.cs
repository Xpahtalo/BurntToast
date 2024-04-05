using System;
using Dalamud.Game.Gui.Toast;
using Dalamud.Game.Text;
using Dalamud.Game.Text.SeStringHandling;
using XivCommon.Functions;

namespace BurntToast;

public class Filter : IDisposable {
    internal Filter(BurntToast plugin) {
        Plugin = plugin;

        Plugin.ToastGui.Toast                           += OnToast;
        Plugin.ToastGui.QuestToast                      += OnQuestToast;
        Plugin.ToastGui.ErrorToast                      += OnErrorToast;
        Plugin.Common.Functions.BattleTalk.OnBattleTalk += OnBattleTalk;
    }

    private BurntToast Plugin { get; }

    public void Dispose() {
        Plugin.Common.Functions.BattleTalk.OnBattleTalk -= OnBattleTalk;
        Plugin.ToastGui.ErrorToast                      -= OnErrorToast;
        Plugin.ToastGui.QuestToast                      -= OnQuestToast;
        Plugin.ToastGui.Toast                           -= OnToast;
    }

    private (bool, string) AnyMatches(string text) {
        var match = Plugin.Config.Patterns.Find(regex => regex.IsMatch(text));
        return (match is not null, match?.ToString() ?? "");
    }

    private void OnToast(ref SeString message, ref ToastOptions options, ref bool isHandled) {
        DoFilter(message, ref isHandled);
    }

    private void OnErrorToast(ref SeString message, ref bool isHandled) {
        DoFilter(message, ref isHandled);
    }

    private void OnQuestToast(ref SeString message, ref QuestToastOptions options, ref bool isHandled) {
        DoFilter(message, ref isHandled);
    }

    private void DoFilter(SeString message, ref bool isHandled) {
        if (isHandled) {
            Plugin.AddHistory(message.TextValue, HistoryType.Toast, HandledType.HandledExternally);
            return;
        }

        var (matched, regex) = AnyMatches(message.TextValue);
        if (matched) {
            Plugin.AddHistory(message.TextValue, HistoryType.Toast, HandledType.Blocked, regex);
            isHandled = true;
            return;
        }

        Plugin.AddHistory(message.TextValue, HistoryType.Toast, HandledType.Passed);
    }

    private void OnBattleTalk(ref SeString sender, ref SeString message, ref BattleTalkOptions options,
                              ref bool     isHandled) {
        var text = message.TextValue;
        
        if (isHandled) {
            Plugin.AddHistory(text, HistoryType.BattleTalk, HandledType.HandledExternally);
            return;
        }
        
        var pattern = Plugin.Config.BattleTalkPatterns.Find(pattern => pattern.Pattern.IsMatch(text));
        if (pattern == null) {
            Plugin.AddHistory(text, HistoryType.BattleTalk, HandledType.Passed);
            return;
        }

        isHandled = true;
        Plugin.AddHistory(text, HistoryType.BattleTalk, HandledType.Blocked, pattern.Pattern.ToString());

        if (pattern.ShowMessage) {
            Plugin.ChatGui.Print(new XivChatEntry {
                Type    = (XivChatType)68,
                Name    = sender.TextValue,
                Message = message,
            });
        }
    }
}