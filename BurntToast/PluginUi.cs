using System;
using System.Globalization;
using System.Linq;
using System.Numerics;
using System.Text.RegularExpressions;
using Dalamud.Interface.Windowing;
using ImGuiNET;

namespace BurntToast;

public sealed class SettingsUi(BurntToast plugin) : Window("BurntToast Settings") {
    private BurntToast Plugin { get; } = plugin;


    public override void Draw() {
        ImGui.SetNextWindowSize(new Vector2(450, 200), ImGuiCond.FirstUseEver);

        if (!ImGui.BeginTabBar("burnt-toast-tabs")) {
            return;
        }

        if (ImGui.BeginTabItem("Toasts")) {
            DrawToastTab();
            ImGui.EndTabItem();
        }

        if (ImGui.BeginTabItem("Battle talk")) {
            DrawBattleTalkTab();
            ImGui.EndTabItem();
        }

        ImGui.EndTabBar();
    }

    private void DrawToastTab() {
        ImGui.PushTextWrapPos();
        ImGui.TextUnformatted(
            "Add regular expressions to filter below. Any toast matching a regular expression on the list will be hidden.");
        ImGui.PopTextWrapPos();

        if (ImGui.Button("Add")) {
            Plugin.Config.Patterns.Add(new Regex(""));
        }

        ImGui.Separator();

        int? toRemove = null;

        var inputWidth = -(ImGui.CalcTextSize("Delete") + ImGui.GetStyle().FramePadding * 4).X;
        for (var i = 0; i < Plugin.Config.Patterns.Count; i++) {
            var pattern     = Plugin.Config.Patterns[i];
            var patternText = pattern.ToString();

            ImGui.PushItemWidth(inputWidth);
            var textResult = ImGui.InputText($"##pattern-{i}", ref patternText, 250);
            ImGui.PopItemWidth();

            ImGui.SameLine();
            if (ImGui.Button($"Delete##{i}")) {
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
                ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(1f, 0f, 0f, 1f));
                ImGui.TextUnformatted("Invalid regular expression.");
                ImGui.PopStyleColor();
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
        ImGui.PushTextWrapPos();
        ImGui.TextUnformatted(
            "Add regular expressions to filter below. Any battle talk matching a regular expression on the list will be hidden.");
        ImGui.PopTextWrapPos();

        if (ImGui.Button("Add")) {
            Plugin.Config.BattleTalkPatterns.Add(new BattleTalkPattern(new Regex(""), true));
        }

        ImGui.Separator();

        int? toRemove = null;

        for (var i = 0; i < Plugin.Config.BattleTalkPatterns.Count; i++) {
            var pattern     = Plugin.Config.BattleTalkPatterns[i];
            var patternText = pattern.Pattern.ToString();
            var textResult  = ImGui.InputText($"##pattern-{i}", ref patternText, 250);

            ImGui.SameLine();
            var show = pattern.ShowMessage;
            if (ImGui.Checkbox("Show in chat", ref show)) {
                pattern.ShowMessage = show;
                Plugin.Config.Save();
            }

            ImGui.SameLine();
            if (ImGui.Button($"Delete##{i}")) {
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
                ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(1f, 0f, 0f, 1f));
                ImGui.TextUnformatted("Invalid regular expression.");
                ImGui.PopStyleColor();
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
}

public sealed class HistoryUi(BurntToast plugin) : Window("Toast History") {
    private static readonly Vector4 Passed            = new(0f, 1f, 0f, 1f);
    private static readonly Vector4 HandledExternally = new(1f, 1f, 0f, 1f);
    private static readonly Vector4 Blocked           = new(1f, 0f, 0f, 1f);


    private BurntToast Plugin { get; } = plugin;

    public override void Draw() {
        ImGui.PushTextWrapPos();

        ImGui.TextUnformatted("Mouse over a toast for details. CTRL+Click to add a Regex for it.");
        if (ImGui.IsItemHovered()) {
            ImGui.BeginTooltip();
            ColorEntry("Displayed.",                                 Passed);
            ColorEntry("Ignored because another plugin handled it.", HandledExternally);
            ColorEntry("Blocked",                                    Blocked);
            ImGui.EndTooltip();
        }

        ImGui.PopTextWrapPos();

        ImGui.Separator();

        ImGui.BeginTabBar("burnt-toast-history-tabs");

        if (ImGui.BeginTabItem("Toasts")) {
            DrawToastHistory();
            ImGui.EndTabItem();
        }

        if (ImGui.BeginTabItem("Battle Talk")) {
            DrawBattleTalkHistory();
            ImGui.EndTabItem();
        }

        ImGui.EndTabBar();
    }

    private void DrawToastHistory() {
        ImGui.PushTextWrapPos();
        foreach (var (historyEntry, i) in Plugin.ToastHistory.Select((x, i) => (x, i))) {
            ImGui.PushID(i);
            DrawHistoryEntry(historyEntry);
            ImGui.PopID();
        }

        ImGui.PopTextWrapPos();
    }

    private void DrawBattleTalkHistory() {
        ImGui.PushTextWrapPos();
        foreach (var (historyEntry, i) in Plugin.BattleTalkHistory.Select((x, i) => (x, i))) {
            ImGui.PushID(i);
            DrawHistoryEntry(historyEntry);
            ImGui.PopID();
        }

        ImGui.PopTextWrapPos();
    }

    private void DrawHistoryEntry(HistoryEntry entry) {
        var color = entry.HandledType switch {
            HandledType.HandledExternally => HandledExternally,
            HandledType.Blocked           => Blocked,
            _                             => Passed,
        };

        ColorEntry(entry.Message, color);

        if (ImGui.IsItemClicked() && (ImGui.IsKeyDown(ImGuiKey.LeftCtrl) || ImGui.IsKeyDown(ImGuiKey.RightCtrl))) {
            if (entry is ToastHistoryEntry) {
                Plugin.Config.BattleTalkPatterns.Add(
                    new BattleTalkPattern(new Regex(Regex.Escape(entry.Message)), true));
            }

            if (entry is BattleTalkHistoryEntry) {
                Plugin.Config.Patterns.Add(new Regex(Regex.Escape(entry.Message)));
            }
        }

        if (ImGui.IsItemHovered()) {
            ImGui.BeginTooltip();
            ImGui.TextUnformatted(entry.Timestamp.ToLocalTime().ToString(CultureInfo.CurrentCulture));
            if (entry is BattleTalkHistoryEntry battleEntry) {
                ImGui.TextUnformatted($"Spoken by: {battleEntry.Sender}");
            }

            if (entry.HandledType == HandledType.Blocked) {
                ImGui.TextUnformatted($"Blocked by: {entry.Regex}");
            }

            ImGui.EndTooltip();
        }
    }

    private static void ColorEntry(string text, Vector4 color) {
        ImGui.PushStyleColor(ImGuiCol.Text, color);
        ImGui.TextUnformatted(text);
        ImGui.PopStyleColor();
    }
}