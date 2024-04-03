using System;
using System.Numerics;
using System.Text.RegularExpressions;
using ImGuiNET;

namespace BurntToast;

public class PluginUi : IDisposable {
    private bool _showSettings;

    public PluginUi(BurntToast plugin) {
        Plugin = plugin;

        Plugin.Interface.UiBuilder.Draw         += Draw;
        Plugin.Interface.UiBuilder.OpenConfigUi += OnOpenConfig;
    }

    private BurntToast Plugin { get; }

    private bool ShowSettings {
        get => _showSettings;
        set => _showSettings = value;
    }

    public void Dispose() {
        Plugin.Interface.UiBuilder.OpenConfigUi -= OnOpenConfig;
        Plugin.Interface.UiBuilder.Draw         -= Draw;
    }

    internal void ToggleConfig() {
        ShowSettings = !ShowSettings;
    }

    private void OnOpenConfig() {
        ShowSettings = true;
    }

    private void Draw() {
        if (!ShowSettings) {
            return;
        }

        ImGui.SetNextWindowSize(new Vector2(450, 200), ImGuiCond.FirstUseEver);

        if (!ImGui.Begin($"{BurntToast.Name} settings", ref _showSettings)) {
            ImGui.End();
            return;
        }

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


        ImGui.End();
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

        for (var i = 0; i < Plugin.Config.Patterns.Count; i++) {
            var pattern     = Plugin.Config.Patterns[i];
            var patternText = pattern.ToString();
            var textResult  = ImGui.InputText($"##pattern-{i}", ref patternText, 250);

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
        }
    }
}