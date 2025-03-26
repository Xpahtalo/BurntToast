using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Dalamud.Game.Gui.Toast;
using Dalamud.Game.Text;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Hooking;
using Dalamud.Memory;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.UI;
using static FFXIVClientStructs.FFXIV.Client.UI.RaptureAtkModule.Delegates;
using static FFXIVClientStructs.FFXIV.Client.UI.UIModule.Delegates;
using InteropGenerator.Runtime;

namespace BurntToast;

public sealed class Filter : IDisposable {
    private readonly Hook<ShowBattleTalk>?      _showBattleTalkHook;
    private readonly Hook<ShowBattleTalkImage>? _showBattleTalkImageHook;
    private readonly Hook<ShowBattleTalkSound>? _showBattleTalkSoundHook;
    private readonly Hook<ShowTextGimmickHint>? _showTextGimmickHint;

    private BurntToast Plugin  { get; }
    private History    History { get; }

    internal unsafe Filter(BurntToast plugin, History history, IGameInteropProvider interop) {
        Plugin                     =  plugin;
        History                    =  history;
        Plugin.ToastGui.Toast      += OnToast;
        Plugin.ToastGui.QuestToast += OnQuestToast;
        Plugin.ToastGui.ErrorToast += OnErrorToast;

        try {
            _showBattleTalkHook = interop.HookFromAddress<ShowBattleTalk>(UIModule.StaticVirtualTablePointer->ShowBattleTalk, ShowBattleTalk);
            _showBattleTalkHook.Enable();
        } catch (Exception ex) {
            Plugin.Log.Error(ex, "Failed to hook ShowBattleTalk. Some battle talks may not get filtered.");
            throw;
        }

        try {
            _showBattleTalkImageHook = interop.HookFromAddress<ShowBattleTalkImage>(
                UIModule.StaticVirtualTablePointer->ShowBattleTalkImage, ShowBattleTalkImage);
            _showBattleTalkImageHook.Enable();
        } catch (Exception ex) { Plugin.Log.Error(ex, "Failed to hook ShowBattleTalkImage. Some battle talks may not get filtered."); }

        try {
            _showBattleTalkSoundHook = interop.HookFromAddress<ShowBattleTalkSound>(
                UIModule.StaticVirtualTablePointer->ShowBattleTalkSound, ShowBattleTalkSound);
            _showBattleTalkSoundHook.Enable();
        } catch (Exception ex) { Plugin.Log.Error(ex, "Failed to hook ShowBattleTalkSound. Some battle talks may not get filtered."); }

        try {
            _showTextGimmickHint = interop.HookFromAddress<ShowTextGimmickHint>(
                RaptureAtkModule.MemberFunctionPointers.ShowTextGimmickHint, ShowTextGimmickHint);
            _showTextGimmickHint.Enable();
        } catch (Exception ex) { Plugin.Log.Error(ex, "Failed to hook ConvertLogMessageIdToScreenLogKind. LogMessage toasts will not get filtered."); }
    }

    public void Dispose() {
        Plugin.ToastGui.ErrorToast -= OnErrorToast;
        Plugin.ToastGui.QuestToast -= OnQuestToast;
        Plugin.ToastGui.Toast      -= OnToast;

        _showBattleTalkHook?.Dispose();
        _showBattleTalkImageHook?.Dispose();
        _showBattleTalkSoundHook?.Dispose();
        _showTextGimmickHint?.Dispose();
    }

    private unsafe void ShowBattleTalk(UIModule* self, CStringPointer sender, CStringPointer talk, float duration, byte style) {
        if (!ApplyBattleTalkFilter(sender, talk, TalkType.Standard)) { _showBattleTalkHook!.Original(self, sender, talk, duration, style); }
    }

    private unsafe void ShowBattleTalkImage(UIModule* self, CStringPointer sender, CStringPointer talk, float duration, uint image, byte style, int sound, uint entityId) {
        if (!ApplyBattleTalkFilter(sender, talk, TalkType.Image)) {
            _showBattleTalkImageHook!.Original(self, sender, talk, duration, image, style, sound, entityId);
        }
    }

    private unsafe void ShowBattleTalkSound(UIModule* self, CStringPointer sender, CStringPointer talk, float duration, int sound, byte style) {
        if (!ApplyBattleTalkFilter(sender, talk, TalkType.Sound)) { _showBattleTalkSoundHook!.Original(self, sender, talk, duration, sound, style); }
    }

    private void OnToast(ref SeString message, ref ToastOptions options, ref bool isHandled) {
        ApplyToastFilter(message.TextValue, ref isHandled);
    }

    private void OnErrorToast(ref SeString message, ref bool isHandled) {
        ApplyToastFilter(message.TextValue, ref isHandled);
    }

    private void OnQuestToast(ref SeString message, ref QuestToastOptions options, ref bool isHandled) {
        ApplyToastFilter(message.TextValue, ref isHandled);
    }

    private bool ApplyBattleTalkFilter(string sender, string talk, TalkType type) {
        var shouldBlock = false;

        try {
            var (pattern, handled, showMessage) = FindBattleTalkMatch(talk, Plugin.Config.BattleTalkPatterns);
            var history = new BattleTalkHistoryEntry(sender, talk, DateTime.UtcNow, handled, pattern);
            History.AddBattleTalkHistory(history);

            if (showMessage) { Plugin.ChatGui.Print(new XivChatEntry { Type = (XivChatType)68, Name = sender, Message = talk, }); }

            shouldBlock = handled == HandledType.Blocked;
        } catch (Exception ex) { Plugin.Log.Error(ex, $"Failed to handle BattleTalk of type {type}"); }

        return shouldBlock;
    }

    private void ApplyToastFilter(string message, ref bool isHandled) {
        if (isHandled) {
            History.AddToastHistory(new ToastHistoryEntry(message, DateTime.UtcNow, HandledType.HandledExternally, ""));
            return;
        }

        var (pattern, handled) = FindPatternMatch(message, Plugin.Config.Patterns);
        var history = new ToastHistoryEntry(message, DateTime.UtcNow, handled, pattern);
        History.AddToastHistory(history);
        isHandled = handled == HandledType.Blocked;
    }

    private unsafe void ShowTextGimmickHint(RaptureAtkModule* self, CStringPointer text, RaptureAtkModule.TextGimmickHintStyle style, int duration) {
        var message = MemoryHelper.ReadSeStringNullTerminated(new IntPtr(text)).TextValue;

        var (pattern, handled) = FindPatternMatch(message, Plugin.Config.GimmickPatterns);
        var history = new GimmickHistoryEntry(message, DateTime.UtcNow, handled, pattern);
        History.AddGimmickHistory(history);

        if (handled == HandledType.Passed) { _showTextGimmickHint!.Original(self, text, style, duration); }
    }

    internal static (string pattern, HandledType, bool showMessage) FindBattleTalkMatch(string message, IEnumerable<BattleTalkPattern> patterns) {
        foreach (var pattern in patterns) {
            if (PatternMatches(message, pattern.Pattern)) { return (pattern.Pattern.ToString(), HandledType.Blocked, pattern.ShowMessage); }
        }
        return ("", HandledType.Passed, false);
    }

    internal static (string pattern, HandledType) FindPatternMatch(string message, IEnumerable<Regex> patterns) {
        foreach (var pattern in patterns) {
            if (PatternMatches(message, pattern)) { return (pattern.ToString(), HandledType.Blocked); }
        }
        return ("", HandledType.Passed);
    }

    internal static bool PatternMatches(string message, Regex pattern) {
        return !string.IsNullOrWhiteSpace(pattern.ToString()) && pattern.IsMatch(message);
    }

    private enum TalkType {
        Standard, Image, Sound,
    }
}