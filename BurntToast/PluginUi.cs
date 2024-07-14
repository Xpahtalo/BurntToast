using System;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Text.RegularExpressions;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Interface.Windowing;
using Dalamud.Utility;
using ImGuiNET;

namespace BurntToast;

public sealed class SettingsUi(BurntToast plugin) : Window("BurntToast Settings") {
    private static readonly string     _delete     = "Delete";
    private static readonly string     _showInChat = "Show in chat";
    private                 BurntToast Plugin { get; } = plugin;

    public override void Draw() {
        ImGui.SetNextWindowSize(new Vector2(450, 200), ImGuiCond.FirstUseEver);

        using var tabBar = ImRaii.TabBar("burnt-toast-tabs");
        if (!tabBar) {
            return;
        }

        DrawToastTab();
        DrawBattleTalkTab();
    }

    private void DrawToastTab() {
        using var toastTab = ImRaii.TabItem("Toasts");
        if (!toastTab) {
            return;
        }

        ImGui.PushTextWrapPos();
        ImGui.TextUnformatted(
            "Add regular expressions to filter below. Any toast matching a regular expression on the list will be hidden.");
        ImGui.PopTextWrapPos();

        if (ImGui.Button("Add")) {
            Plugin.Config.AddToastPattern("");
        }

        ImGui.Separator();

        int? toRemove = null;

        var inputWidth = -GetButtonSize(_delete).X;
        for (var i = 0; i < Plugin.Config.Patterns.Count; i++) {
            var pattern     = Plugin.Config.Patterns[i];
            var patternText = pattern.ToString();

            ImGui.PushItemWidth(inputWidth);
            var textResult = ImGui.InputText($"##pattern-{i}", ref patternText, 250);
            ImGui.PopItemWidth();

            ImGui.SameLine();
            if (ImGui.Button($"{_delete}##{i}")) {
                toRemove = i;
            }

            if (!textResult) {
                continue;
            }

            if (string.IsNullOrWhiteSpace(patternText)) {
                continue;
            }

            Regex? regex = null;
            try {
                regex = new Regex(patternText, RegexOptions.Compiled);
            }
            catch (ArgumentException) {
                using var style = ImRaii.PushColor(ImGuiCol.Text, new Vector4(1f, 0f, 0f, 1f));
                ImGui.TextUnformatted("Invalid regular expression.");
            }

            if (regex == null) {
                continue;
            }

            Plugin.Config.Patterns[i] = regex;
            Plugin.Config.Save();
        }

        if (toRemove != null) {
            Plugin.Config.Patterns.RemoveAt(toRemove.Value);
            Plugin.Config.Save();
        }
    }

    private void DrawBattleTalkTab() {
        using var battleTalkTab = ImRaii.TabItem("Battle Talk");
        if (!battleTalkTab) {
            return;
        }

        ImGui.PushTextWrapPos();
        ImGui.TextUnformatted(
            "Add regular expressions to filter below. Any battle talk matching a regular expression on the list will be hidden.");
        ImGui.PopTextWrapPos();

        if (ImGui.Button("Add")) {
            Plugin.Config.AddBattleTalkPattern("", true);
        }

        ImGui.Separator();

        int? toRemove = null;

        var deleteButtonSize = GetButtonSize(_delete);
        var showCheckboxSize = GetCheckboxSize(_showInChat);
        var inputWidth       = -(deleteButtonSize.X + showCheckboxSize.X);
        for (var i = 0; i < Plugin.Config.BattleTalkPatterns.Count; i++) {
            var pattern     = Plugin.Config.BattleTalkPatterns[i];
            var patternText = pattern.Pattern.ToString();

            ImGui.PushItemWidth(inputWidth);
            var textResult = ImGui.InputText($"##pattern-{i}", ref patternText, 250);
            ImGui.PopItemWidth();

            ImGui.SameLine();
            var show = pattern.ShowMessage;
            if (ImGui.Checkbox(_showInChat, ref show)) {
                pattern.ShowMessage = show;
                Plugin.Config.Save();
            }

            ImGui.SameLine();
            if (ImGui.Button($"{_delete}##{i}")) {
                toRemove = i;
            }

            if (!textResult) {
                continue;
            }

            if (string.IsNullOrWhiteSpace(patternText)) {
                continue;
            }

            Regex? regex = null;
            try {
                regex = new Regex(patternText, RegexOptions.Compiled);
            }
            catch (ArgumentException) {
                using var style = ImRaii.PushColor(ImGuiCol.Text, new Vector4(1f, 0f, 0f, 1f));
                ImGui.TextUnformatted("Invalid regular expression.");
            }

            if (regex == null) {
                continue;
            }

            pattern.Pattern = regex;
            Plugin.Config.Save();
        }

        if (toRemove != null) {
            Plugin.Config.BattleTalkPatterns.RemoveAt(toRemove.Value);
            Plugin.Config.Save();
        }
    }

    private static Vector2 GetButtonSize(string text) {
        return ImGui.CalcTextSize(text) + ImGui.GetStyle().FramePadding * 4;
    }

    private static Vector2 GetCheckboxSize(string text) {
        var textSize = ImGui.CalcTextSize(text);
        return textSize with { X = textSize.X + ImGui.GetStyle().FramePadding.X * 3 + ImGui.GetFrameHeight(), };
    }
}

public sealed class HistoryUi(BurntToast plugin, History history) : Window("Toast History") {
    private const           string  BlockedTooltip    = "\nBlocked by: ";
    private const           string  SenderTooltip     = "\nSpoken by: ";
    private static readonly Vector4 Passed            = new(0f, 1f, 0f, 1f);
    private static readonly Vector4 HandledExternally = new(1f, 1f, 0f, 1f);
    private static readonly Vector4 Blocked           = new(1f, 0f, 0f, 1f);

    private BurntToast Plugin { get; } = plugin;

    public override void Draw() {
        ImGui.PushTextWrapPos();

        ImGui.TextUnformatted("Mouse over a toast for details. CTRL+Click to add a Regex for it.");
        if (ImGui.IsItemHovered()) {
            ImGui.BeginTooltip();
            ImGuiExtensions.TextColored("Was not blocked by any regex.",              Passed);
            ImGuiExtensions.TextColored("Ignored because another plugin handled it.", HandledExternally);
            ImGuiExtensions.TextColored("Was blocked by a regex.",                    Blocked);
            ImGui.EndTooltip();
        }

        ImGui.PopTextWrapPos();

        ImGui.Separator();

        using var tabBar = ImRaii.TabBar("burnt-toast-history-tabs");
        if (!tabBar) {
            return;
        }

        DrawToastHistory();
        DrawBattleTalkHistory();
    }

    private void DrawToastHistory() {
        using var toastTab = ImRaii.TabItem("Toasts");
        if (!toastTab) {
            return;
        }

        ImGui.PushTextWrapPos();
        foreach (var (historyEntry, i) in history.ToastHistory.Reverse().Select((x, i) => (x, i))) {
            ImGui.PushID(i);
            var tooltip = new StringBuilder();
            tooltip.Append(historyEntry.Timestamp);
            if (historyEntry.HandledType == HandledType.Blocked) {
                tooltip.Append(BlockedTooltip);
                tooltip.Append(historyEntry.Regex);
            }

            if (DrawHistoryEntry(historyEntry.Message, tooltip.ToString(), historyEntry.HandledType)) {
                Plugin.Config.AddToastPattern(EscapeRegex(historyEntry.Message));
            }

            ImGui.PopID();
        }

        ImGui.PopTextWrapPos();
    }

    private void DrawBattleTalkHistory() {
        using var battleTalkTab = ImRaii.TabItem("Battle Talk");
        if (!battleTalkTab) {
            return;
        }

        ImGui.PushTextWrapPos();
        foreach (var (historyEntry, i) in history.BattleTalkHistory.Reverse().Select((x, i) => (x, i))) {
            ImGui.PushID(i);
            var tooltip = new StringBuilder();
            tooltip.Append(historyEntry.Timestamp);

            if (!historyEntry.Sender.IsNullOrWhitespace()) {
                tooltip.Append(SenderTooltip);
                tooltip.Append(historyEntry.Sender);
            }

            if (historyEntry.HandledType == HandledType.Blocked) {
                tooltip.Append(BlockedTooltip);
                tooltip.Append(historyEntry.Regex);
            }

            if (DrawHistoryEntry(historyEntry.Message, tooltip.ToString(), historyEntry.HandledType)) {
                Plugin.Config.AddBattleTalkPattern(EscapeRegex(historyEntry.Message), true);
            }

            ImGui.PopID();
        }

        ImGui.PopTextWrapPos();
    }

    private static bool DrawHistoryEntry(string text, string tooltip, HandledType handledType) {
        var color = handledType switch {
            HandledType.HandledExternally => HandledExternally,
            HandledType.Blocked           => Blocked,
            _                             => Passed,
        };

        ImGuiExtensions.TextColored(text, color);

        var clicked = ImGui.IsItemClicked() &&
                      (ImGui.IsKeyDown(ImGuiKey.LeftCtrl) || ImGui.IsKeyDown(ImGuiKey.RightCtrl));

        if (ImGui.IsItemHovered()) {
            ImGui.BeginTooltip();
            ImGui.TextUnformatted(tooltip);
            ImGui.EndTooltip();
        }

        return clicked;
    }

    // Not using Regex.Escape because it escapes whitespace, which is unnecessary for our use-case, and ugly for the user.
    private static string EscapeRegex(ReadOnlySpan<char> input) {
        var sb = new StringBuilder(input.Length * 2);
        foreach (var ch in input) {
            switch (ch) {
                case '\\':
                    sb.Append(@"\\");
                    break;
                case '*':
                    sb.Append(@"\*");
                    break;
                case '+':
                    sb.Append(@"\+");
                    break;
                case '?':
                    sb.Append(@"\?");
                    break;
                case '|':
                    sb.Append(@"\|");
                    break;
                case '{':
                    sb.Append(@"\{");
                    break;
                case '}':
                    sb.Append(@"\}");
                    break;
                case '[':
                    sb.Append(@"\[");
                    break;
                case ']':
                    sb.Append(@"\]");
                    break;
                case '(':
                    sb.Append(@"\(");
                    break;
                case ')':
                    sb.Append(@"\)");
                    break;
                case '^':
                    sb.Append(@"\^");
                    break;
                case '$':
                    sb.Append(@"\$");
                    break;
                case '.':
                    sb.Append(@"\.");
                    break;
                case '#':
                    sb.Append(@"\#");
                    break;
                default:
                    sb.Append(ch);
                    break;
            }
        }

        return sb.ToString();
    }
}