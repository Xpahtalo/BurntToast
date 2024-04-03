using System;
using System.Linq;
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

    private bool AnyMatches(string text) {
        return Plugin.Config.Patterns.Any(regex => regex.IsMatch(text));
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
            return;
        }

        if (AnyMatches(message.TextValue)) {
            isHandled = true;
        }
    }

    private void OnBattleTalk(ref SeString sender, ref SeString message, ref BattleTalkOptions options,
                              ref bool     isHandled) {
        if (isHandled) {
            return;
        }

        var text    = message.TextValue;
        var pattern = Plugin.Config.BattleTalkPatterns.Find(pattern => pattern.Pattern.IsMatch(text));
        if (pattern == null) {
            return;
        }

        isHandled = true;

        if (pattern.ShowMessage) {
            Plugin.ChatGui.Print(new XivChatEntry {
                Type    = (XivChatType)68,
                Name    = sender.TextValue,
                Message = message,
            });
        }
    }
}