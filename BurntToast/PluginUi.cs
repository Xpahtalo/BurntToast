using System;
using System.Numerics;
using System.Text.RegularExpressions;
using ImGuiNET;

namespace BurntToast {
    public class PluginUi : IDisposable {
        private BurntToast Plugin { get; }

        private bool _showSettings;

        private bool ShowSettings {
            get => this._showSettings;
            set => this._showSettings = value;
        }

        public PluginUi(BurntToast plugin) {
            this.Plugin = plugin;

            this.Plugin.Interface.UiBuilder.Draw += this.Draw;
            this.Plugin.Interface.UiBuilder.OpenConfigUi += this.OnOpenConfig;
        }

        public void Dispose() {
            this.Plugin.Interface.UiBuilder.OpenConfigUi -= this.OnOpenConfig;
            this.Plugin.Interface.UiBuilder.Draw -= this.Draw;
        }

        internal void ToggleConfig() {
            this.ShowSettings = !this.ShowSettings;
        }

        private void OnOpenConfig() {
            this.ShowSettings = true;
        }

        private void Draw() {
            if (!this.ShowSettings) {
                return;
            }

            ImGui.SetNextWindowSize(new Vector2(450, 200), ImGuiCond.FirstUseEver);

            if (!ImGui.Begin($"{this.Plugin.Name} settings", ref this._showSettings)) {
                ImGui.End();
                return;
            }

            if (!ImGui.BeginTabBar("burnt-toast-tabs")) {
                return;
            }

            if (ImGui.BeginTabItem("Toasts")) {
                this.DrawToastTab();
                ImGui.EndTabItem();
            }

            if (ImGui.BeginTabItem("Battle talk")) {
                this.DrawBattleTalkTab();
                ImGui.EndTabItem();
            }

            ImGui.EndTabBar();



            ImGui.End();
        }

        private void DrawToastTab() {
            ImGui.PushTextWrapPos();
            ImGui.TextUnformatted("Add regular expressions to filter below. Any toast matching a regular expression on the list will be hidden.");
            ImGui.PopTextWrapPos();

            if (ImGui.Button("Add")) {
                this.Plugin.Config.Patterns.Add(new Regex(""));
            }

            ImGui.Separator();

            int? toRemove = null;

            for (var i = 0; i < this.Plugin.Config.Patterns.Count; i++) {
                var pattern = this.Plugin.Config.Patterns[i];
                var patternText = pattern.ToString();
                var textResult = ImGui.InputText($"##pattern-{i}", ref patternText, 250);

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
                } catch (ArgumentException) {
                    ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(1f, 0f, 0f, 1f));
                    ImGui.TextUnformatted("Invalid regular expression.");
                    ImGui.PopStyleColor();
                }

                if (regex == null) {
                    continue;
                }

                this.Plugin.Config.Patterns[i] = regex;
                this.Plugin.Config.Save();
            }

            if (toRemove != null) {
                this.Plugin.Config.Patterns.RemoveAt(toRemove.Value);
            }
        }

        private void DrawBattleTalkTab() {
            ImGui.PushTextWrapPos();
            ImGui.TextUnformatted("Add regular expressions to filter below. Any battle talk matching a regular expression on the list will be hidden.");
            ImGui.PopTextWrapPos();

            if (ImGui.Button("Add")) {
                this.Plugin.Config.BattleTalkPatterns.Add(new BattleTalkPattern(new Regex(""), true));
            }

            ImGui.Separator();

            int? toRemove = null;

            for (var i = 0; i < this.Plugin.Config.BattleTalkPatterns.Count; i++) {
                var pattern = this.Plugin.Config.BattleTalkPatterns[i];
                var patternText = pattern.Pattern.ToString();
                var textResult = ImGui.InputText($"##pattern-{i}", ref patternText, 250);

                ImGui.SameLine();
                var show = pattern.ShowMessage;
                if (ImGui.Checkbox("Show in chat", ref show)) {
                    pattern.ShowMessage = show;
                    this.Plugin.Config.Save();
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
                } catch (ArgumentException) {
                    ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(1f, 0f, 0f, 1f));
                    ImGui.TextUnformatted("Invalid regular expression.");
                    ImGui.PopStyleColor();
                }

                if (regex == null) {
                    continue;
                }

                pattern.Pattern = regex;
                this.Plugin.Config.Save();
            }

            if (toRemove != null) {
                this.Plugin.Config.BattleTalkPatterns.RemoveAt(toRemove.Value);
            }
        }
    }
}
