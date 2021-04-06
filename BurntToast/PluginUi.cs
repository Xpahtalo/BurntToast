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

            this.Plugin.Interface.UiBuilder.OnBuildUi += this.Draw;
            this.Plugin.Interface.UiBuilder.OnOpenConfigUi += this.OnOpenConfig;
        }

        public void Dispose() {
            this.Plugin.Interface.UiBuilder.OnOpenConfigUi -= this.OnOpenConfig;
            this.Plugin.Interface.UiBuilder.OnBuildUi -= this.Draw;
        }

        internal void ToggleConfig() {
            this.ShowSettings = !this.ShowSettings;
        }

        private void OnOpenConfig(object? sender, EventArgs e) {
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

            ImGui.End();
        }
    }
}
