using System;
using Dalamud.Game.Gui.Toast;
using Dalamud.Game.Text;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Hooking;
using Dalamud.Memory;
using FFXIVClientStructs.FFXIV.Client.UI;
using static FFXIVClientStructs.FFXIV.Client.UI.UIModule.Delegates;

namespace BurntToast;

public sealed class Filter : IDisposable {
    private BurntToast Plugin  { get; }
    private History    History { get; }

    private readonly Hook<ShowBattleTalk>?      _showBattleTalkHook;
    private readonly Hook<ShowBattleTalkImage>? _showBattleTalkImageHook;
    private readonly Hook<ShowBattleTalkSound>? _showBattleTalkSoundHook;

    internal unsafe Filter(BurntToast plugin, History history) {
        Plugin  = plugin;
        History = history;

        Plugin.ToastGui.Toast      += OnToast;
        Plugin.ToastGui.QuestToast += OnQuestToast;
        Plugin.ToastGui.ErrorToast += OnErrorToast;
        
        try {
            _showBattleTalkHook =
                Plugin.InteropProvider.HookFromAddress<ShowBattleTalk>(
                    UIModule.StaticVirtualTablePointer->ShowBattleTalk, ShowBattleTalk);
            _showBattleTalkHook.Enable();
        }
        catch (Exception ex) {
            Plugin.Log.Error(ex, "Failed to hook ShowBattleTalk. Some battle talks may not get filtered. Please send feedback through the plugin installer.");
            throw;
        }
        
        try {
            _showBattleTalkImageHook =
                Plugin.InteropProvider.HookFromAddress<ShowBattleTalkImage>(
                    UIModule.StaticVirtualTablePointer->ShowBattleTalkImage, ShowBattleTalkImage);
            _showBattleTalkImageHook.Enable();
        }
        catch (Exception ex) {
            Plugin.Log.Error(ex, "Failed to hook ShowBattleTalkImage. Some battle talks may not get filtered. Please send feedback through the plugin installer.");
        }
        
        try {
            _showBattleTalkSoundHook =
                Plugin.InteropProvider.HookFromAddress<ShowBattleTalkSound>(
                    UIModule.StaticVirtualTablePointer->ShowBattleTalkSound, ShowBattleTalkSound);
            _showBattleTalkSoundHook.Enable();
        }
        catch (Exception ex) {
            Plugin.Log.Error(ex, "Failed to hook ShowBattleTalkSound. Some battle talks may not get filtered. Please send feedback through the plugin installer.");
        }
    }

    public void Dispose() {
        _showBattleTalkHook?.Dispose();
        _showBattleTalkImageHook?.Dispose();
        _showBattleTalkSoundHook?.Dispose();
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

    private unsafe void ShowBattleTalk(UIModule* self,     byte* sender, byte* talk,
                                       float     duration, byte  style) {
        if (!InnerShowBattleTalkDetour(sender, talk, TalkType.Standard)) {
            _showBattleTalkHook!.Original(self, sender, talk, duration, style);
        }
    }

    private unsafe void ShowBattleTalkImage(UIModule* self,     byte* sender, byte* talk,
                                            float     duration, uint  image,  byte  style, int sound, uint entityId) {
        if (!InnerShowBattleTalkDetour(sender, talk, TalkType.Image)) {
            _showBattleTalkImageHook!.Original(self, sender, talk, duration, image, style, sound, entityId);
        }
    }

    private unsafe void ShowBattleTalkSound(UIModule* self,     byte* sender, byte* talk,
                                            float     duration, int   sound,  byte  style) {
        if (!InnerShowBattleTalkDetour(sender, talk, TalkType.Sound)) {
            _showBattleTalkSoundHook!.Original(self, sender, talk, duration, sound, style);
        }
    }

    private unsafe bool InnerShowBattleTalkDetour(byte* sender, byte* talk, TalkType type) {
        var shouldBlock = false;
        try {
            shouldBlock = ShouldBlock(new IntPtr(sender), new IntPtr(talk));
        }
        catch (Exception ex) {
            Plugin.Log.Error(ex, $"Failed to handle BattleTalk of type {type}");
        }

        return shouldBlock;
    }

    private bool ShouldBlock(IntPtr sender, IntPtr talk) {
        var senderValue = MemoryHelper.ReadSeStringNullTerminated(sender).TextValue;
        var talkValue   = MemoryHelper.ReadSeStringNullTerminated(talk).TextValue;

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

    private enum TalkType {
        Standard,
        Image,
        Sound,
    }
}