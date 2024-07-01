using System;
using Dalamud.Game.Gui.Toast;
using Dalamud.Game.Text;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Hooking;
using FFXIVClientStructs.FFXIV.Client.UI;
using static FFXIVClientStructs.FFXIV.Client.UI.UIModule.Delegates;

namespace BurntToast;

public sealed class Filter : IDisposable {
    private BurntToast Plugin  { get; }
    private History    History { get; }

    private readonly Hook<ShowBattleTalk>      _showBattleTalkHook;
    private readonly Hook<ShowBattleTalkImage> _showBattleTalkImageHook;
    private readonly Hook<ShowBattleTalkSound> _showBattleTalkSoundHook;

    internal unsafe Filter(BurntToast plugin, History history) {
        Plugin  = plugin;
        History = history;

        Plugin.ToastGui.Toast      += OnToast;
        Plugin.ToastGui.QuestToast += OnQuestToast;
        Plugin.ToastGui.ErrorToast += OnErrorToast;
        _showBattleTalkHook =
            Plugin.InteropProvider.HookFromAddress<ShowBattleTalk>(
                UIModule.StaticVirtualTablePointer->ShowBattleTalk, ShowBattleTalk);
        _showBattleTalkImageHook =
            Plugin.InteropProvider.HookFromAddress<ShowBattleTalkImage>(
                UIModule.StaticVirtualTablePointer->ShowBattleTalkImage, ShowBattleTalkImage);
        _showBattleTalkSoundHook =
            Plugin.InteropProvider.HookFromAddress<ShowBattleTalkSound>(
                UIModule.StaticVirtualTablePointer->ShowBattleTalkSound, ShowBattleTalkSound);
        _showBattleTalkHook.Enable();
        _showBattleTalkImageHook.Enable();
        _showBattleTalkSoundHook.Enable();
    }

    public void Dispose() {
        _showBattleTalkHook.Dispose();
        _showBattleTalkImageHook.Dispose();
        _showBattleTalkSoundHook.Dispose();
        Plugin.ToastGui.ErrorToast -= OnErrorToast;
        Plugin.ToastGui.QuestToast -= OnQuestToast;
        Plugin.ToastGui.Toast      -= OnToast;
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
            History.AddToastHistory(message.TextValue, HandledType.HandledExternally);
            return;
        }

        var (matched, regex) = AnyMatches(message.TextValue);
        if (matched) {
            History.AddToastHistory(message.TextValue, HandledType.Blocked, regex);
            isHandled = true;
            return;
        }

        History.AddToastHistory(message.TextValue, HandledType.Passed);
    }

    private unsafe void ShowBattleTalk(UIModule* self, byte* sender, byte* talk, float duration, byte style) {
        Plugin.Log.Debug("Intercepting BattleTalk");

        var shouldBlock = false;
        try {
            shouldBlock = ShouldBlock(sender, talk);
        }
        catch (Exception ex) {
            Plugin.Log.Error(ex, "Failed to handle ShowBattleTalk");
        }
        finally {
            if (!shouldBlock) {
                _showBattleTalkHook.Original(self, sender, talk, duration, style);
            }
        }
    }

    private unsafe void ShowBattleTalkImage(UIModule* self, byte* sender, byte* talk, float duration, uint image,
                                            byte      style) {
        Plugin.Log.Debug("Intercepting BattleTalkImage");

        var shouldBlock = false;
        try {
            shouldBlock = ShouldBlock(sender, talk);
        }
        catch (Exception ex) {
            Plugin.Log.Error(ex, "Failed to handle BattleTalkImage");
        }
        finally {
            if (!shouldBlock) {
                _showBattleTalkImageHook.Original(self, sender, talk, duration, image, style);
            }
        }
    }

    private unsafe void ShowBattleTalkSound(UIModule* self, byte* sender, byte* talk, float duration, int sound,
                                            byte      style) {
        Plugin.Log.Debug("Intercepting BattleTalkSound");

        var shouldBlock = false;
        try {
            shouldBlock = ShouldBlock(sender, talk);
        }
        catch (Exception ex) {
            Plugin.Log.Error(ex, "Failed to handle BattleTalkSound");
        }
        finally {
            if (!shouldBlock) {
                _showBattleTalkSoundHook.Original(self, sender, talk, duration, sound, style);
            }
        }
    }

    private static unsafe int GetLength(byte* s) {
        var l = 0;
        while (s[l] != '\0') {
            l += 1;
        }

        return l;
    }

    private unsafe bool ShouldBlock(byte* sender, byte* talk) {
        var senderValue = SeString.Parse(sender, GetLength(sender)).TextValue;
        var talkValue   = SeString.Parse(talk,   GetLength(talk)).TextValue;

        var pattern = Plugin.Config.BattleTalkPatterns.Find(pattern => pattern.Pattern.IsMatch(talkValue));
        var matched = pattern is not null;

        var history = new BattleTalkHistoryEntry(
            senderValue,
            talkValue,
            DateTime.UtcNow,
            matched ? HandledType.Blocked : HandledType.Passed,
            pattern?.Pattern.ToString() ?? string.Empty);

        History.AddBattleTalkHistory(history);

        if (pattern is { ShowMessage: true, }) {
            Plugin.ChatGui.Print(new XivChatEntry {
                Type    = (XivChatType)68,
                Name    = senderValue,
                Message = talkValue,
            });
        }

        return matched;
    }
}